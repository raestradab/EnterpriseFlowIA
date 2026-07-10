using EnterpriseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseFlow.Infrastructure.Persistence.Configurations;

public sealed class AssistantMessageConfiguration : IEntityTypeConfiguration<AssistantMessage>
{
    public void Configure(EntityTypeBuilder<AssistantMessage> builder)
    {
        builder.ToTable("AssistantMessages");

        builder.HasKey(m => m.Id);

        // No HasMaxLength — an assistant's answer can genuinely run long; nvarchar(max) is the
        // right default here, unlike EventName/Message-style short fields elsewhere.
        builder.Property(m => m.Content)
            .IsRequired();

        builder.Property(m => m.CreatedBy)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(m => m.ModifiedBy)
            .HasMaxLength(100);

        // HU-091: "recuerde el contexto de mi conversación actual" — loading a user's own
        // message history in order is the only real read pattern.
        builder.HasIndex(m => new { m.TenantId, m.UserId, m.CreatedAtUtc });
    }
}
