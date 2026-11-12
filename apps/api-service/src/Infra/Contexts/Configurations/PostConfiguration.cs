using Aurore.Blueprints.ApiService.Core.Domain.Entities;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Configuration;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Extensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurore.Blueprints.ApiService.Infra.Contexts.Configurations;

internal sealed class PostConfiguration : EntityConfigurationBase<Post>
{
    public const string UniqueSlugIndex = "ux_posts_slug";

    protected override void Extend(EntityTypeBuilder<Post> builder)
    {
        builder.Property(x => x.Slug).HasMaxLength(80);
        builder.Property(x => x.Title).HasMaxLength(160);
        builder.Property(x => x.Body).HasMaxLength(20000);
        builder.Property(x => x.Status).HasConversion<string>().HasMaxLength(16);

        // A deleted post gives its slug back.
        builder.HasIndex(x => x.Slug).IsUnique().WhereNotDeleted().HasDatabaseName(UniqueSlugIndex);
        builder.HasIndex(x => new { x.Status, x.PublishedAt });

        builder
            .HasOne(x => x.Author)
            .WithMany()
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
