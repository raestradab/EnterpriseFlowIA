using EnterpriseFlow.Domain.Enums;

namespace EnterpriseFlow.Domain.Events;

/// <summary>
/// Raised by <see cref="Entities.Document.TransitionTo"/> on every successful transition, not
/// only "meaningful" ones — <see cref="Entities.Document"/> doesn't know which state names are
/// business-significant (the workflow is generic and tenant-configurable, ADR-0010), so it
/// can't decide what's "an approval" itself. <c>ToStateName</c> is a fact injected by
/// Application (same reasoning as <c>isTransitionAllowed</c>) precisely so a future
/// notification handler (F6, ADR-0011) can filter on it without <see cref="Entities.Document"/>
/// needing to encode that decision.
/// </summary>
public sealed record DocumentWorkflowTransitionedDomainEvent(
    Guid DocumentId,
    Guid ToStateId,
    string ToStateName,
    DocumentOwnerType OwnerType,
    Guid OwnerId) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
