using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.ProjectTasks.GetMyCalendar;

public sealed class GetMyCalendarQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetMyCalendarQuery, IReadOnlyCollection<CalendarItemDto>>
{
    public async Task<IReadOnlyCollection<CalendarItemDto>> Handle(
        GetMyCalendarQuery request,
        CancellationToken cancellationToken) =>
        await db.ProjectTasks
            .Where(t => t.AssignedToUserId == currentUser.UserId)
            .Where(t => t.DueDate != null && t.DueDate >= request.From && t.DueDate <= request.To)
            .Select(t => new CalendarItemDto(t.Id, t.Title, t.ProjectId, t.DueDate!.Value))
            .ToListAsync(cancellationToken);
}
