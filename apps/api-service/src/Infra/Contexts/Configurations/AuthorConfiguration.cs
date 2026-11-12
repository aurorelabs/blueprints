using Aurore.Blueprints.ApiService.Core.Domain.Entities;
using Aurore.Foundation.EntityFrameworkCore.Postgres.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Aurore.Blueprints.ApiService.Infra.Contexts.Configurations;

internal sealed class AuthorConfiguration : EntityConfigurationBase<Author>
{
    public const string UniqueSubjectIndex = "ux_authors_subject";
    public const string UniqueHandleIndex = "ux_authors_handle";

    protected override void Extend(EntityTypeBuilder<Author> builder)
    {
        builder.Property(x => x.Subject).HasMaxLength(200);
        builder.Property(x => x.Handle).HasMaxLength(32);
        builder.Property(x => x.DisplayName).HasMaxLength(80);

        builder.HasIndex(x => x.Subject).IsUnique().HasDatabaseName(UniqueSubjectIndex);
        builder.HasIndex(x => x.Handle).IsUnique().HasDatabaseName(UniqueHandleIndex);
    }
}
