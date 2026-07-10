using EnterpriseFlow.Domain.Common;
using EnterpriseFlow.Domain.Exceptions;

namespace EnterpriseFlow.Domain.Entities;

/// <summary>
/// F1.3/HU-002/HU-003. Owns its Role assignments (<see cref="UserRoleAssignment"/>), the same
/// pattern as <see cref="Project"/> owning <see cref="ProjectMember"/> (ADR-0005). Stores only
/// the password *hash* — hashing/verification is an Infrastructure concern
/// (<c>IPasswordHasher</c>, Application/Abstractions), Domain never sees a plaintext password.
/// </summary>
public sealed class User : BaseEntity, ITenantScoped, IAuditableEntity, ISoftDeletable
{
    private readonly List<UserRoleAssignment> _roleAssignments = [];

    private User()
    {
        Email = string.Empty;
        PasswordHash = string.Empty;
        CreatedBy = string.Empty;
    }

    public string Email { get; private set; }

    public string PasswordHash { get; private set; }

    public IReadOnlyCollection<UserRoleAssignment> RoleAssignments => _roleAssignments.AsReadOnly();

    public Guid TenantId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public static User Create(string email, string passwordHash)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email is required.", nameof(email));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("Password hash is required.", nameof(passwordHash));
        }

        return new User
        {
            Email = email.Trim().ToLowerInvariant(),
            PasswordHash = passwordHash,
        };
    }

    public void AssignTenant(Guid tenantId) => TenantId = tenantId;

    public void AssignRole(Guid roleId)
    {
        if (_roleAssignments.Any(r => r.RoleId == roleId))
        {
            throw new UserAlreadyHasRoleException(Id, roleId);
        }

        _roleAssignments.Add(UserRoleAssignment.Create(Id, roleId));
    }

    public void RemoveRole(Guid roleId) => _roleAssignments.RemoveAll(r => r.RoleId == roleId);

    public void MarkDeleted()
    {
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
    }
}
