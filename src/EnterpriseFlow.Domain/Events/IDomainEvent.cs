namespace EnterpriseFlow.Domain.Events;

/// <summary>
/// Marker interface for domain events. Deliberately has no dependency on MediatR or any
/// dispatch mechanism — Domain must not depend on Application/Infrastructure (ADR-0002).
/// Application is responsible for translating these into its own notification pipeline.
/// </summary>
public interface IDomainEvent
{
    DateTimeOffset OccurredOnUtc { get; }
}
