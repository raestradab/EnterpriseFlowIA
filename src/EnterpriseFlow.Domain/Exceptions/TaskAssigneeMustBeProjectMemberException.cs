namespace EnterpriseFlow.Domain.Exceptions;

/// <summary>HU-023: a Task can only be assigned to a user who belongs to its Project's team.</summary>
public sealed class TaskAssigneeMustBeProjectMemberException(Guid taskId, Guid userId)
    : DomainException($"User '{userId}' is not a member of the project team and cannot be assigned to task '{taskId}'.")
{
}
