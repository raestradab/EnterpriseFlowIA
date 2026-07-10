namespace EnterpriseFlow.Domain.Exceptions;

/// <summary>HU-021: a Project cannot close while it still has open (non-terminal) Tasks.</summary>
public sealed class ProjectHasOpenTasksException(Guid projectId)
    : DomainException($"Project '{projectId}' cannot be closed because it still has open tasks.")
{
}
