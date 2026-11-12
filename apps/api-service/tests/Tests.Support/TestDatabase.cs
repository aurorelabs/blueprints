using System;
using System.Threading;
using System.Threading.Tasks;
using Aurore.Blueprints.ApiService.Infra.Contexts;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Extensions;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Tests.Support;

/// <summary>
/// A database of its own on the PostgreSQL server of the machine (127.0.0.1:5432, user and password "postgres", the same the
/// development settings use): created for the test class and dropped afterwards, so the server is shared by every project and
/// nothing of the tests is left on it. The schema is made from the model. Fails with a hint when the server is not there.
/// </summary>
public sealed class TestDatabase : IAsyncDisposable
{
    private const string ServerHint = "The tests need a PostgreSQL server on 127.0.0.1:5432 with the user and password 'postgres': the sandbox infrastructure has one (see docs/instructions/testing.md).";

    private TestDatabase(string name, PostgresOptions options)
    {
        Name = name;
        Options = options;
    }

    public string Name { get; }

    /// <summary>The settings of this database, as the application reads them from its configuration.</summary>
    public PostgresOptions Options { get; }

    public static async Task<TestDatabase> CreateAsync(CancellationToken cancellationToken)
    {
        var name = $"blueprints_{Guid.NewGuid():N}";

        try
        {
            await using var connection = new NpgsqlConnection(Server("postgres").ToConnectionString());
            await connection.OpenAsync(cancellationToken);
            await using var create = new NpgsqlCommand($"CREATE DATABASE \"{name}\"", connection);
            await create.ExecuteNonQueryAsync(cancellationToken);
        }
        catch (NpgsqlException ex)
        {
            throw new InvalidOperationException($"Could not reach the test PostgreSQL. {ServerHint}", ex);
        }

        var options = Server(name);

        var contextOptions = new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(options.ToConnectionString()).Options;
        await using var context = new AppDbContext(contextOptions);
        await context.Database.EnsureCreatedAsync(cancellationToken);

        return new TestDatabase(name, options);
    }

    public async ValueTask DisposeAsync()
    {
        NpgsqlConnection.ClearAllPools();

        await using var connection = new NpgsqlConnection(Server("postgres").ToConnectionString());
        await connection.OpenAsync();
        await using var drop = new NpgsqlCommand($"DROP DATABASE IF EXISTS \"{Name}\" WITH (FORCE)", connection);
        await drop.ExecuteNonQueryAsync();
    }

    // 127.0.0.1 and not "localhost": it resolves to ::1 first, and docker in WSL does not forward IPv6.
    private static PostgresOptions Server(string database)
    {
        return new PostgresOptions
        {
            ["Host"] = Environment.GetEnvironmentVariable("TEST_POSTGRES_HOST") ?? "127.0.0.1",
            ["Port"] = "5432",
            ["Database"] = database,
            ["Username"] = "postgres",
            ["Password"] = "postgres",
            ["Timeout"] = "5",
        };
    }
}
