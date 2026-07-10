using EnterpriseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EnterpriseFlow.Infrastructure.Persistence.Configurations;

public sealed class ProjectMemberConfiguration : IEntityTypeConfiguration<ProjectMember>
{
    public void Configure(EntityTypeBuilder<ProjectMember> builder)
    {
        builder.ToTable("ProjectMembers");

        builder.HasKey(m => m.Id);

        // Defense in depth: Project.AddMember already rejects duplicates in memory: this
        // unique index rejects them at the database level too, e.g. against concurrent writes
        // from two requests that both loaded the Project before either saved.
        builder.HasIndex(m => new { m.ProjectId, m.UserId }).IsUnique();
    }
}
