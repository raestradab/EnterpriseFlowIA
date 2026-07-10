using EnterpriseFlow.Domain.Events;

namespace EnterpriseFlow.Domain.Common;

/// <summary>
/// Base class for entities/aggregate roots. Owns the domain events raised by the
/// aggregate; Application dequeues and dispatches them after a successful SaveChanges.
/// </summary>
public abstract class BaseEntity
{
    private readonly List<IDomainEvent> _domainEvents = [];

    public Guid Id { get; protected set; } = Guid.NewGuid();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void ClearDomainEvents() => _domainEvents.Clear();

    protected void Raise(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);
}
