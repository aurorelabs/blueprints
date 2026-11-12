using Aurore.Blueprints.ApiService.Core.Domain.Entities;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurore.Blueprints.ApiService.Infra.Contexts.Configurations;

internal sealed class CommentConfiguration : EntityConfigurationBase<Comment>
{
    protected override void Extend(EntityTypeBuilder<Comment> builder)
    {
        builder.Property(x => x.Body).HasMaxLength(2000);

        // Comments are listed by post, oldest or newest first.
        builder.HasIndex(x => new { x.PostId, x.CreatedAt });

        builder
            .HasOne(x => x.Post)
            .WithMany(x => x.Comments)
            .HasForeignKey(x => x.PostId)
            .OnDelete(DeleteBehavior.Restrict);

        builder
            .HasOne(x => x.Author)
            .WithMany()
            .HasForeignKey(x => x.AuthorId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
