using EnterpriseFlow.Domain.Common;
using EnterpriseFlow.Domain.Exceptions;

namespace EnterpriseFlow.Domain.Entities;

/// <summary>
/// F1.4 (Gestión de Roles y Permisos). Tenant-scoped — HU-003 requires roles/permission
/// mappings to be configurable *per tenant*, not a single global set (ADR-0004).
/// </summary>
public sealed class Role : BaseEntity, ITenantScoped, IAuditableEntity, ISoftDeletable
{
    private readonly List<RolePermission> _permissions = [];

    private Role()
    {
        Name = string.Empty;
        CreatedBy = string.Empty;
    }

    public string Name { get; private set; }

    public IReadOnlyCollection<RolePermission> Permissions => _permissions.AsReadOnly();

    public Guid TenantId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public static Role Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Role name is required.", nameof(name));
        }

        return new Role
        {
            Name = name.Trim(),
        };
    }

    public void AssignTenant(Guid tenantId) => TenantId = tenantId;

    public void GrantPermission(string permission)
    {
        if (_permissions.Any(p => p.Permission == permission))
        {
            throw new RoleAlreadyHasPermissionException(Id, permission);
        }

        _permissions.Add(RolePermission.Create(Id, permission));
    }

    public void RevokePermission(string permission) => _permissions.RemoveAll(p => p.Permission == permission);

    public bool HasPermission(string permission) => _permissions.Any(p => p.Permission == permission);

    public void MarkDeleted()
    {
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
    }
}
