using EnterpriseFlow.Domain.Common;

namespace EnterpriseFlow.Domain.Entities;

/// <summary>
/// A single permission string (ADR-0004's catalog) granted to a <see cref="Role"/>. Child
/// entity of the Role aggregate, same shape as <see cref="ProjectMember"/>/
/// <see cref="UserRoleAssignment"/>.
/// </summary>
public sealed class RolePermission : BaseEntity
{
    private RolePermission()
    {
        Permission = string.Empty;
    }

    public Guid RoleId { get; private set; }

    public string Permission { get; private set; }

    internal static RolePermission Create(Guid roleId, string permission) => new()
    {
        RoleId = roleId,
        Permission = permission,
    };
}
