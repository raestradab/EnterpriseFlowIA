namespace EnterpriseFlow.Domain.Exceptions;

/// <summary>HU-003: a permission cannot be granted twice to the same Role.</summary>
public sealed class RoleAlreadyHasPermissionException(Guid roleId, string permission)
    : DomainException($"Role '{roleId}' already has permission '{permission}' granted.")
{
}
