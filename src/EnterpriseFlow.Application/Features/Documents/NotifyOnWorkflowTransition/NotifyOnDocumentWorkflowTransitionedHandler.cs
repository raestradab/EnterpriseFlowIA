using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Enums;
using EnterpriseFlow.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Documents.NotifyOnWorkflowTransition;

/// <summary>
/// F6 (ADR-0011): reacts to <see cref="DocumentWorkflowTransitionedDomainEvent"/> — the same
/// domain-events pipeline <c>CascadeContactsOnClientDeactivatedHandler</c> already uses, no new
/// message bus. Recipients are the members of the owning Project (HU-081's actual use case: a
/// reviewer needs to know a Document moved). A Task-owned Document resolves to its Project's
/// members the same way; a Client-owned Document has no "members" concept in this domain at all
/// (Clients aren't staffed the way Projects are) — no HU asks for that case, so it silently
/// yields zero recipients rather than inventing a rule nobody requested.
/// </summary>
public sealed class NotifyOnDocumentWorkflowTransitionedHandler(
    IAppDbContext db,
    IRealtimeNotifier realtimeNotifier,
    IEmailQueue emailQueue)
    : INotificationHandler<DomainEventNotification<DocumentWorkflowTransitionedDomainEvent>>
{
    public async Task Handle(DomainEventNotification<DocumentWorkflowTransitionedDomainEvent> notification, CancellationToken cancellationToken)
    {
        var domainEvent = notification.DomainEvent;
        var recipientUserIds = await ResolveRecipientUserIdsAsync(db, domainEvent.OwnerType, domainEvent.OwnerId, cancellationToken);

        if (recipientUserIds.Count == 0)
        {
            return;
        }

        var message = $"El documento cambió al estado '{domainEvent.ToStateName}'.";

        foreach (var userId in recipientUserIds)
        {
            db.Notifications.Add(Notification.Create(userId, "document.transitioned", message));
        }

        await db.SaveChangesAsync(cancellationToken);

        var recipients = await db.Users
            .Where(u => recipientUserIds.Contains(u.Id))
            .Select(u => new { u.Id, u.Email })
            .ToListAsync(cancellationToken);

        foreach (var recipient in recipients)
        {
            await realtimeNotifier.NotifyUserAsync(
                recipient.Id,
                "document.transitioned",
                new { domainEvent.DocumentId, domainEvent.ToStateName },
                cancellationToken);

            emailQueue.Enqueue(recipient.Email, "Documento actualizado", message);
        }
    }

    private static async Task<List<Guid>> ResolveRecipientUserIdsAsync(
        IAppDbContext db, DocumentOwnerType ownerType, Guid ownerId, CancellationToken cancellationToken)
    {
        var projectId = ownerType switch
        {
            DocumentOwnerType.Project => (Guid?)ownerId,
            DocumentOwnerType.Task => await db.ProjectTasks
                .Where(t => t.Id == ownerId)
                .Select(t => (Guid?)t.ProjectId)
                .FirstOrDefaultAsync(cancellationToken),
            _ => null,
        };

        if (projectId is null)
        {
            return [];
        }

        return await db.Projects
            .Where(p => p.Id == projectId)
            .SelectMany(p => p.Members)
            .Select(m => m.UserId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }
}
