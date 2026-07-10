using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.ProjectTasks.GetTasks;

public sealed class GetTasksQueryHandler(IAppDbContext db)
    : IRequestHandler<GetTasksQuery, IReadOnlyCollection<TaskListItemDto>>
{
    public async Task<IReadOnlyCollection<TaskListItemDto>> Handle(GetTasksQuery request, CancellationToken cancellationToken)
    {
        var query = db.ProjectTasks.AsQueryable();

        if (request.ProjectId is { } projectId)
        {
            query = query.Where(t => t.ProjectId == projectId);
        }

        return await query
            .OrderBy(t => t.DueDate)
            .Select(t => new TaskListItemDto(t.Id, t.Title, t.Status, t.Priority, t.ProjectId, t.AssignedToUserId, t.DueDate))
            .ToListAsync(cancellationToken);
    }
}
