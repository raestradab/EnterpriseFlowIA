using EnterpriseFlow.Domain.Events;
using MediatR;

namespace EnterpriseFlow.Application.Common;

/// <summary>
/// Bridges a Domain event (<see cref="IDomainEvent"/>, which deliberately has no dependency on
/// MediatR — ADR-0002) into MediatR's notification pipeline. Infrastructure wraps and publishes
/// one of these per domain event after a successful <c>SaveChanges</c> (see AppDbContext) —
/// Application handlers subscribe to <c>INotificationHandler&lt;DomainEventNotification&lt;TEvent&gt;&gt;</c>.
/// </summary>
public sealed record DomainEventNotification<TDomainEvent>(TDomainEvent DomainEvent) : INotification
    where TDomainEvent : IDomainEvent;
