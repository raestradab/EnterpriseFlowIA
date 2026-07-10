using EnterpriseFlow.Domain.Common;
using EnterpriseFlow.Domain.Exceptions;

namespace EnterpriseFlow.Domain.Entities;

/// <summary>
/// F8.1 (Motor de Workflow genérico) — Release 2, Sprint 5. See ADR-0010 for why this is a
/// data-driven state machine (<see cref="WorkflowState"/>/<see cref="WorkflowTransition"/> as
/// rows, not a hardcoded enum + methods like <see cref="Project"/>/<see cref="ProjectTask"/>
/// use) — its whole reason to exist is that F8.1 requires a tenant to define/change flows
/// without a code change, unlike Release 1's fixed status enums. Participant entities (e.g.
/// <see cref="Document"/>) reference their current state by id and ask
/// <see cref="CanTransition"/> before attempting a move — the same "hecho inyectado" pattern
/// ADR-0005 already established, applied here to workflow transitions instead of cross-aggregate
/// facts like open-task counts.
/// </summary>
public sealed class WorkflowDefinition : BaseEntity, ITenantScoped, IAuditableEntity, ISoftDeletable
{
    private readonly List<WorkflowState> _states = [];
    private readonly List<WorkflowTransition> _transitions = [];

    private WorkflowDefinition()
    {
        Name = string.Empty;
        CreatedBy = string.Empty;
    }

    public string Name { get; private set; }

    public IReadOnlyCollection<WorkflowState> States => _states.AsReadOnly();

    public IReadOnlyCollection<WorkflowTransition> Transitions => _transitions.AsReadOnly();

    public Guid TenantId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public static WorkflowDefinition Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Workflow name is required.", nameof(name));
        }

        return new WorkflowDefinition
        {
            Name = name.Trim(),
        };
    }

    public void AssignTenant(Guid tenantId) => TenantId = tenantId;

    public WorkflowState AddState(string name, bool isInitial, bool isFinal)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("State name is required.", nameof(name));
        }

        if (isInitial && _states.Any(s => s.IsInitial))
        {
            throw new DuplicateInitialWorkflowStateException(Id);
        }

        var state = WorkflowState.Create(Id, name.Trim(), isInitial, isFinal);
        _states.Add(state);
        return state;
    }

    public WorkflowTransition AddTransition(string name, Guid fromStateId, Guid toStateId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Transition name is required.", nameof(name));
        }

        if (_states.All(s => s.Id != fromStateId))
        {
            throw new UnknownWorkflowStateException(Id, fromStateId);
        }

        if (_states.All(s => s.Id != toStateId))
        {
            throw new UnknownWorkflowStateException(Id, toStateId);
        }

        if (_transitions.Any(t => t.FromStateId == fromStateId && t.ToStateId == toStateId))
        {
            throw new DuplicateWorkflowTransitionException(Id, fromStateId, toStateId);
        }

        var transition = WorkflowTransition.Create(Id, name.Trim(), fromStateId, toStateId);
        _transitions.Add(transition);
        return transition;
    }

    /// <summary>The fact a participant entity (e.g. <see cref="Document"/>) needs before
    /// attempting <c>TransitionTo</c> — resolved by Application, injected as a bool, never
    /// queried by the participant itself (ADR-0005/ADR-0010).</summary>
    public bool CanTransition(Guid fromStateId, Guid toStateId) =>
        _transitions.Any(t => t.FromStateId == fromStateId && t.ToStateId == toStateId);

    public void MarkDeleted()
    {
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
    }
}
