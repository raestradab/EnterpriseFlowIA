using EnterpriseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseFlow.Infrastructure.Persistence.Configurations;

public sealed class WorkflowDefinitionConfiguration : IEntityTypeConfiguration<WorkflowDefinition>
{
    public void Configure(EntityTypeBuilder<WorkflowDefinition> builder)
    {
        builder.ToTable("WorkflowDefinitions");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(w => w.CreatedBy)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(w => w.ModifiedBy)
            .HasMaxLength(100);

        builder.HasIndex(w => new { w.TenantId, w.Name }).IsUnique();

        builder.HasMany(w => w.States)
            .WithOne()
            .HasForeignKey(s => s.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(w => w.States)
            .HasField("_states")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(w => w.Transitions)
            .WithOne()
            .HasForeignKey(t => t.WorkflowDefinitionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(w => w.Transitions)
            .HasField("_transitions")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
