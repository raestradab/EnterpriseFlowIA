using EnterpriseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseFlow.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Email)
            .HasMaxLength(256)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .IsRequired();

        builder.Property(u => u.CreatedBy)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(u => u.ModifiedBy)
            .HasMaxLength(100);

        // Global uniqueness (not per-tenant) at the database level too — mirrors the
        // application-level check in RegisterTenantCommandHandler (ADR-0006). A unique index
        // ignores the query filter by nature (it's a storage constraint, not a query), so this
        // still catches races the application check alone wouldn't.
        builder.HasIndex(u => u.Email).IsUnique();

        builder.HasMany(u => u.RoleAssignments)
            .WithOne()
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(u => u.RoleAssignments)
            .HasField("_roleAssignments")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
