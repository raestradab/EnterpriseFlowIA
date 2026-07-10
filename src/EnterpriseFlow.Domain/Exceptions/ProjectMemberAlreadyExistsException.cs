namespace EnterpriseFlow.Domain.Exceptions;

/// <summary>HU-022: a user cannot be added twice to the same Project's team.</summary>
public sealed class ProjectMemberAlreadyExistsException(Guid projectId, Guid userId)
    : DomainException($"User '{userId}' is already a member of project '{projectId}'.")
{
}
