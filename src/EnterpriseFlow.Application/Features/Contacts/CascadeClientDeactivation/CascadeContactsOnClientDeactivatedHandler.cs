using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Contacts.CascadeClientDeactivation;

/// <summary>
/// HU-012: when a Client is deactivated, its Contacts must cascade to inactive too. Reacts to
/// the domain event <see cref="Client.Deactivate"/> raises (ADR-0006's sibling concern —
/// Domain can't reach across aggregates itself, so Application does it in response to the
/// event instead).
/// </summary>
public sealed class CascadeContactsOnClientDeactivatedHandler(IAppDbContext db)
    : INotificationHandler<DomainEventNotification<ClientDeactivatedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<ClientDeactivatedDomainEvent> notification, CancellationToken cancellationToken)
    {
        var contacts = await db.Contacts
            .Where(c => c.ClientId == notification.DomainEvent.ClientId)
            .ToListAsync(cancellationToken);

        foreach (var contact in contacts)
        {
            contact.MarkDeleted();
        }

        if (contacts.Count > 0)
        {
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
