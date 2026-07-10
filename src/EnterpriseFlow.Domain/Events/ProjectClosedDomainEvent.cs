namespace EnterpriseFlow.Domain.Events;

/// <summary>
/// Raised by <see cref="Entities.Project.Close"/>. Anticipated in the Sprint 2 component
/// design (c4-03-componentes-proyectos.md) as the extension point for Notifications
/// (Release 2) — Sprint 5 only defines and raises it; nothing subscribes yet.
/// </summary>
public sealed record ProjectClosedDomainEvent(Guid ProjectId) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
