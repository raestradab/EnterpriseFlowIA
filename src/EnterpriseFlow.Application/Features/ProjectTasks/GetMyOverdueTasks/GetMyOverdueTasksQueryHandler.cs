using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.ProjectTasks.GetMyOverdueTasks;

public sealed class GetMyOverdueTasksQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetMyOverdueTasksQuery, IReadOnlyCollection<OverdueTaskDto>>
{
    public async Task<IReadOnlyCollection<OverdueTaskDto>> Handle(
        GetMyOverdueTasksQuery request, CancellationToken cancellationToken)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        return await db.ProjectTasks
            .Where(t => t.AssignedToUserId == currentUser.UserId)
            .Where(t => t.DueDate != null && t.DueDate < today)
            .Where(t => t.Status != ProjectTaskStatus.Completed && t.Status != ProjectTaskStatus.Cancelled)
            .Select(t => new OverdueTaskDto(t.Id, t.Title, t.ProjectId, t.DueDate!.Value, t.Status))
            .ToListAsync(cancellationToken);
    }
}
