using EnterpriseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseFlow.Infrastructure.Persistence.Configurations;

public sealed class DocumentChunkConfiguration : IEntityTypeConfiguration<DocumentChunk>
{
    public void Configure(EntityTypeBuilder<DocumentChunk> builder)
    {
        builder.ToTable("DocumentChunks");

        builder.HasKey(c => c.Id);

        // No HasMaxLength — a chunk's extracted text can run long; nvarchar(max) is the right
        // default, same reasoning as AssistantMessage.Content.
        builder.Property(c => c.Content)
            .IsRequired();

        builder.Property(c => c.Embedding)
            .IsRequired();

        builder.Property(c => c.CreatedBy)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(c => c.ModifiedBy)
            .HasMaxLength(100);

        // No FK on DocumentId (ADR-0009/ADR-0005 — cross-aggregate reference, validated in
        // Application, not the database). ADR-0014: retrieval always scans a tenant's own
        // chunks; DocumentId-scoped lookups matter too (deleting/regenerating a Document's
        // chunks on re-index) — a single composite index covers both, same shape as
        // Document's own (TenantId, OwnerType, OwnerId).
        builder.HasIndex(c => new { c.TenantId, c.DocumentId });
    }
}
