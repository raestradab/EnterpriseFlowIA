namespace EnterpriseFlow.Domain.Events;

/// <summary>
/// Raised by <see cref="Entities.Client.Deactivate"/>. HU-012 requires that a Client's
/// Contacts cascade to inactive when the Client is deactivated — since Contact is a separate
/// aggregate, Domain cannot reach across to update them directly. An Application-layer
/// handler (wired in Sprint 7, when handlers exist) subscribes to this event and soft-deletes
/// the Client's Contacts.
/// </summary>
public sealed record ClientDeactivatedDomainEvent(Guid ClientId) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
