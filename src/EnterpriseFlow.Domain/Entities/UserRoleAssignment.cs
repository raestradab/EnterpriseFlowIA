using EnterpriseFlow.Domain.Common;

namespace EnterpriseFlow.Domain.Entities;

/// <summary>
/// A Role assigned to a User (HU-003). Child entity of the <see cref="User"/> aggregate — same
/// shape as <see cref="ProjectMember"/> within <see cref="Project"/> (ADR-0005's sibling case):
/// no independent lifecycle, construction restricted to the owning aggregate.
/// </summary>
public sealed class UserRoleAssignment : BaseEntity
{
    private UserRoleAssignment()
    {
    }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }

    internal static UserRoleAssignment Create(Guid userId, Guid roleId) => new()
    {
        UserId = userId,
        RoleId = roleId,
    };
}
