using EnterpriseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseFlow.Infrastructure.Persistence.Configurations;

public sealed class DocumentConfiguration : IEntityTypeConfiguration<Document>
{
    public void Configure(EntityTypeBuilder<Document> builder)
    {
        builder.ToTable("Documents");

        builder.HasKey(d => d.Id);

        // 260: the Windows MAX_PATH-derived convention .NET tooling commonly uses for file names.
        builder.Property(d => d.FileName)
            .HasMaxLength(260)
            .IsRequired();

        builder.Property(d => d.ContentType)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.StorageKey)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(d => d.CreatedBy)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(d => d.ModifiedBy)
            .HasMaxLength(100);

        // No FK on OwnerId (ADR-0009, polymorphic owner across Project/Client/ProjectTask —
        // same cross-aggregate reasoning as ADR-0005) nor on CurrentWorkflowStateId (ADR-0010,
        // same reasoning applied to workflow state). Both are validated in Application.

        // Matches the real query shape (the query filter already adds WHERE TenantId = @current;
        // OwnerType/OwnerId narrow it to "documents attached to this Project/Client/Task", the
        // Documents tab a detail page needs).
        builder.HasIndex(d => new { d.TenantId, d.OwnerType, d.OwnerId });
    }
}
