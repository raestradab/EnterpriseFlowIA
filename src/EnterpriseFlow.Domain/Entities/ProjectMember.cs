using EnterpriseFlow.Domain.Common;
using EnterpriseFlow.Domain.Enums;

namespace EnterpriseFlow.Domain.Entities;

/// <summary>
/// A user's membership in a <see cref="Project"/>'s team (HU-022). Child entity of the
/// Project aggregate — no independent lifecycle, no tenant/audit/soft-delete markers of its
/// own; it lives and dies with its owning Project.
/// </summary>
public sealed class ProjectMember : BaseEntity
{
    private ProjectMember()
    {
    }

    public Guid ProjectId { get; private set; }

    public Guid UserId { get; private set; }

    public ProjectRole Role { get; private set; }

    internal static ProjectMember Create(Guid projectId, Guid userId, ProjectRole role) => new()
    {
        ProjectId = projectId,
        UserId = userId,
        Role = role,
    };
}
