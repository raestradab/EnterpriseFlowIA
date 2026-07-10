using EnterpriseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseFlow.Infrastructure.Persistence.Configurations;

public sealed class WorkflowTransitionConfiguration : IEntityTypeConfiguration<WorkflowTransition>
{
    public void Configure(EntityTypeBuilder<WorkflowTransition> builder)
    {
        builder.ToTable("WorkflowTransitions");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name)
            .HasMaxLength(100)
            .IsRequired();

        // Defense in depth: WorkflowDefinition.AddTransition already rejects a duplicate
        // From/To pair in memory — same reasoning as ProjectMemberConfiguration's unique index.
        builder.HasIndex(t => new { t.WorkflowDefinitionId, t.FromStateId, t.ToStateId }).IsUnique();
    }
}
