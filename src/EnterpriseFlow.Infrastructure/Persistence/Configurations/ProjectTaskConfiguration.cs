using EnterpriseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseFlow.Infrastructure.Persistence.Configurations;

public sealed class ProjectTaskConfiguration : IEntityTypeConfiguration<ProjectTask>
{
    public void Configure(EntityTypeBuilder<ProjectTask> builder)
    {
        // F7.9 (HU-102, ADR-0015) — see ProjectConfiguration for the full reasoning.
        builder.ToTable("ProjectTasks", tb => tb.IsTemporal());

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasMaxLength(2000);

        builder.Property(t => t.CreatedBy)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(t => t.ModifiedBy)
            .HasMaxLength(100);

        builder.HasOne<Project>()
            .WithMany()
            .HasForeignKey(t => t.ProjectId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(t => t.ProjectId);
        builder.HasIndex(t => t.AssignedToUserId);

        // Supports the dashboard's "tareas vencidas" indicator (F4.1) and the HU-021 open-tasks
        // check without a full table scan.
        builder.HasIndex(t => new { t.TenantId, t.Status });
    }
}
