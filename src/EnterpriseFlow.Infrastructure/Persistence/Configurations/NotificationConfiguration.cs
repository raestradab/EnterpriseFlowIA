using EnterpriseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseFlow.Infrastructure.Persistence.Configurations;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");

        builder.HasKey(n => n.Id);

        builder.Property(n => n.EventName)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(n => n.Message)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(n => n.CreatedBy)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(n => n.ModifiedBy)
            .HasMaxLength(100);

        // HU-062: "mis notificaciones, leídas y no leídas" — the one real read pattern (a
        // user's own notification center), so TenantId+UserId+IsRead is the shape that matters.
        builder.HasIndex(n => new { n.TenantId, n.UserId, n.IsRead });
    }
}
