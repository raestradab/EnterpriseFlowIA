using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.ProjectTasks.GetTaskHistory;

public sealed class GetTaskHistoryQueryHandler(IAppDbContext db) : IRequestHandler<GetTaskHistoryQuery, TaskHistoryDto?>
{
    public Task<TaskHistoryDto?> Handle(GetTaskHistoryQuery request, CancellationToken cancellationToken) =>
        db.GetProjectTasksAsOf(request.AsOf)
            .Where(t => t.Id == request.TaskId)
            .Select(t => new TaskHistoryDto(t.Id, t.Title, t.Status, t.ProjectId, request.AsOf))
            .SingleOrDefaultAsync(cancellationToken);
}
