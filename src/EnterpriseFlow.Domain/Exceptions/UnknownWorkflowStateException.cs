namespace EnterpriseFlow.Domain.Exceptions;

/// <summary>HU-080: a transition can only reference states that belong to the same
/// WorkflowDefinition — a transition into another Workflow's state would be meaningless.</summary>
public sealed class UnknownWorkflowStateException(Guid workflowDefinitionId, Guid stateId)
    : DomainException($"Workflow '{workflowDefinitionId}' has no state '{stateId}'.")
{
}
