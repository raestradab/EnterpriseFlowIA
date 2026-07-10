using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.ProjectTasks.GetTaskById;

public sealed class GetTaskByIdQueryHandler(IAppDbContext db) : IRequestHandler<GetTaskByIdQuery, TaskDto?>
{
    public Task<TaskDto?> Handle(GetTaskByIdQuery request, CancellationToken cancellationToken) =>
        db.ProjectTasks
            .Where(t => t.Id == request.Id)
            .Select(t => new TaskDto(t.Id, t.Title, t.Description, t.Priority, t.Status, t.ProjectId, t.AssignedToUserId, t.DueDate))
            .FirstOrDefaultAsync(cancellationToken);
}
