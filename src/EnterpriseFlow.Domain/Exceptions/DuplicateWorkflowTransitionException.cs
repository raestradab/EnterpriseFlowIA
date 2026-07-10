namespace EnterpriseFlow.Domain.Exceptions;

/// <summary>HU-080: two transitions between the same pair of states would be redundant — if
/// they need different names/behavior, that's a modeling sign they're actually different states.</summary>
public sealed class DuplicateWorkflowTransitionException(Guid workflowDefinitionId, Guid fromStateId, Guid toStateId)
    : DomainException($"Workflow '{workflowDefinitionId}' already has a transition from '{fromStateId}' to '{toStateId}'.")
{
}
