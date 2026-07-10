using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Notifications.GetMyNotifications;

public sealed class GetMyNotificationsQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetMyNotificationsQuery, IReadOnlyCollection<NotificationDto>>
{
    public async Task<IReadOnlyCollection<NotificationDto>> Handle(GetMyNotificationsQuery request, CancellationToken cancellationToken)
    {
        // Sorted client-side, not via OrderBy in the query: SQL Server translates ORDER BY over
        // a datetimeoffset column natively, but SQLite (used by the integration test suite,
        // ADR — see docs/09-pruebas.md) has no native DateTimeOffset type and can't translate it
        // server-side — this was caught by that exact test, not assumed up front. A per-user
        // notification list is small enough that sorting after materializing is a non-issue.
        var notifications = await db.Notifications
            .Where(n => n.UserId == currentUser.UserId)
            .Select(n => new NotificationDto(n.Id, n.EventName, n.Message, n.IsRead, n.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return notifications.OrderByDescending(n => n.CreatedAtUtc).ToList();
    }
}
