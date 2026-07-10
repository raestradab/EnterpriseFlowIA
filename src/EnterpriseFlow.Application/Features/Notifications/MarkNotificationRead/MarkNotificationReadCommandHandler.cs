using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Notifications.MarkNotificationRead;

/// <summary>The lookup filters by <c>UserId == currentUser.UserId</c> in the same query as the
/// existence check, not as a separate "is this mine?" step after — a notification that exists
/// but belongs to someone else must 404 exactly like one that doesn't exist at all, or the
/// response itself would leak which notification IDs are real (IDOR).</summary>
public sealed class MarkNotificationReadCommandHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<MarkNotificationReadCommand>
{
    public async Task Handle(MarkNotificationReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await db.Notifications
            .FirstOrDefaultAsync(n => n.Id == request.Id && n.UserId == currentUser.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Notification), request.Id);

        notification.MarkRead();

        await db.SaveChangesAsync(cancellationToken);
    }
}
