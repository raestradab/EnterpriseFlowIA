using EnterpriseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseFlow.Infrastructure.Persistence.Configurations;

public sealed class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
{
    public void Configure(EntityTypeBuilder<RolePermission> builder)
    {
        builder.ToTable("RolePermissions");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Permission)
            .HasMaxLength(100)
            .IsRequired();

        builder.HasIndex(p => new { p.RoleId, p.Permission }).IsUnique();
    }
}
