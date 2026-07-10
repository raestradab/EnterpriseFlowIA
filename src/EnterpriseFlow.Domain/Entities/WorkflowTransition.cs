using EnterpriseFlow.Domain.Common;

namespace EnterpriseFlow.Domain.Entities;

/// <summary>
/// A single allowed state change of a <see cref="WorkflowDefinition"/> (F8.1, ADR-0010) — the
/// data that makes the engine "configurable without a code change" instead of a hardcoded state
/// machine. Constructed only through <see cref="WorkflowDefinition.AddTransition"/>.
/// </summary>
public sealed class WorkflowTransition : BaseEntity
{
    private WorkflowTransition()
    {
        Name = string.Empty;
    }

    public Guid WorkflowDefinitionId { get; private set; }

    public string Name { get; private set; }

    public Guid FromStateId { get; private set; }

    public Guid ToStateId { get; private set; }

    internal static WorkflowTransition Create(Guid workflowDefinitionId, string name, Guid fromStateId, Guid toStateId) => new()
    {
        WorkflowDefinitionId = workflowDefinitionId,
        Name = name,
        FromStateId = fromStateId,
        ToStateId = toStateId,
    };
}
