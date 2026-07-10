using EnterpriseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseFlow.Infrastructure.Persistence.Configurations;

public sealed class ProjectConfiguration : IEntityTypeConfiguration<Project>
{
    public void Configure(EntityTypeBuilder<Project> builder)
    {
        // F7.9 (HU-102, ADR-0015): SQL Server System-Versioned Temporal Table — SQL Server
        // maintains ProjectsHistory automatically on every UPDATE/DELETE, no application code
        // populates or can bypass it. PeriodStart/PeriodEnd are shadow properties EF Core adds
        // on its own; Domain's Project class doesn't need to know they exist.
        builder.ToTable("Projects", tb => tb.IsTemporal());

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.CreatedBy)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.ModifiedBy)
            .HasMaxLength(100);

        builder.HasOne<Client>()
            .WithMany()
            .HasForeignKey(p => p.ClientId)
            .OnDelete(DeleteBehavior.Restrict);

        // Project owns its team membership (ADR-0005) — mapped through the private backing
        // field since Members is exposed as a read-only collection, not a settable list.
        builder.HasMany(p => p.Members)
            .WithOne()
            .HasForeignKey(m => m.ProjectId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(p => p.Members)
            .HasField("_members")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasIndex(p => new { p.TenantId, p.Name });
        builder.HasIndex(p => p.ClientId);
    }
}
