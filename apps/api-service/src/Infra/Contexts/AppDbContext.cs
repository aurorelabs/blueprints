using Aurore.Blueprints.ApiService.Core.Domain.Entities;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Aurore.Blueprints.ApiService.Infra.Contexts;

/// <summary>
/// The database of the application. Entities are added here as DbSet properties with their configuration classes
/// (EntityConfigurationBase) in this assembly; the foundation conventions are applied last.
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Author>? Authors { get; init; }

    public DbSet<Post>? Posts { get; init; }

    public DbSet<Comment>? Comments { get; init; }

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder
            .ApplyConfigurationsFromAssembly(typeof(IInfra).Assembly)
            .ApplyAuroreConventions();
    }
}
