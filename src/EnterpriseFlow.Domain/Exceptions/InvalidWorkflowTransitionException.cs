namespace EnterpriseFlow.Domain.Exceptions;

/// <summary>HU-080/HU-081: rejects a state transition the owning WorkflowDefinition doesn't
/// define — the transition being valid is a fact Application resolves and injects
/// (ADR-0005/ADR-0010), never something the participant entity checks itself.</summary>
public sealed class InvalidWorkflowTransitionException(Guid entityId, Guid targetStateId)
    : DomainException($"Entity '{entityId}' cannot transition to workflow state '{targetStateId}' — no such transition is defined.")
{
}
