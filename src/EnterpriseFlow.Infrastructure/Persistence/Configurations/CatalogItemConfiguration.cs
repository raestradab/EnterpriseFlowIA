using EnterpriseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseFlow.Infrastructure.Persistence.Configurations;

public sealed class CatalogItemConfiguration : IEntityTypeConfiguration<CatalogItem>
{
    public void Configure(EntityTypeBuilder<CatalogItem> builder)
    {
        builder.ToTable("CatalogItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.Key)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(i => i.Label)
            .HasMaxLength(200)
            .IsRequired();

        // Defense in depth: CatalogDefinition.AddItem already rejects duplicate keys in memory
        // (same reasoning as ProjectMemberConfiguration's unique index) — this catches races
        // from two requests that both loaded the Catalog before either saved.
        builder.HasIndex(i => new { i.CatalogDefinitionId, i.Key }).IsUnique();
    }
}
