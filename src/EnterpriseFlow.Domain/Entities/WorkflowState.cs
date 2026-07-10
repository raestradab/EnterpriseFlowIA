using EnterpriseFlow.Domain.Common;

namespace EnterpriseFlow.Domain.Entities;

/// <summary>
/// A single state of a <see cref="WorkflowDefinition"/> (F8.1, ADR-0010). Child entity,
/// constructed only through <see cref="WorkflowDefinition.AddState"/> — same shape as
/// <see cref="RolePermission"/>/<see cref="CatalogItem"/>.
/// </summary>
public sealed class WorkflowState : BaseEntity
{
    private WorkflowState()
    {
        Name = string.Empty;
    }

    public Guid WorkflowDefinitionId { get; private set; }

    public string Name { get; private set; }

    public bool IsInitial { get; private set; }

    public bool IsFinal { get; private set; }

    internal static WorkflowState Create(Guid workflowDefinitionId, string name, bool isInitial, bool isFinal) => new()
    {
        WorkflowDefinitionId = workflowDefinitionId,
        Name = name,
        IsInitial = isInitial,
        IsFinal = isFinal,
    };
}
