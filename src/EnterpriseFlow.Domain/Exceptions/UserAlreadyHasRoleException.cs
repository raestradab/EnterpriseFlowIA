namespace EnterpriseFlow.Domain.Exceptions;

/// <summary>HU-003: a Role cannot be assigned twice to the same User.</summary>
public sealed class UserAlreadyHasRoleException(Guid userId, Guid roleId)
    : DomainException($"User '{userId}' already has role '{roleId}' assigned.")
{
}
