namespace EnterpriseFlow.Domain.Exceptions;

/// <summary>HU-080: a WorkflowDefinition has exactly one entry point — a second "initial" state
/// would make "where does a new instance start?" ambiguous.</summary>
public sealed class DuplicateInitialWorkflowStateException(Guid workflowDefinitionId)
    : DomainException($"Workflow '{workflowDefinitionId}' already has an initial state.")
{
}
