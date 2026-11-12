using System;
using Aurore.Blueprints.ApiService.Infra.Contexts;
using Aurore.Foundation.Core.Extensions;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Aurore.Blueprints.ApiService.Tools.Migrations;

/// <summary>
/// How the EF tools build the DbContext to read the model and to run migrations. The connection string is the first
/// argument after the "--" of the dotnet ef command.
/// </summary>
public sealed class MigrationContext : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <inheritdoc />
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString = args.Length > 0 ? args[0] : null;

        if (connectionString.HasValue() is false)
            throw new InvalidOperationException("The connection string is missing. Pass it after '--': dotnet ef migrations add Name --project tools/Tools.Migrations -- \"Host=...;Database=...\"");

        var builder = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(connectionString, x =>
            {
                x.MigrationsHistoryTable("migrations", "history");
                x.MigrationsAssembly(typeof(MigrationContext).Assembly.FullName);
            });

        builder.UseSnakeCaseNpgsqlHistoryTable();

        return new AppDbContext(builder.Options);
    }
}
