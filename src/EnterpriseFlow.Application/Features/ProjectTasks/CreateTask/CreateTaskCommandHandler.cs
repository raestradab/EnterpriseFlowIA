using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Domain.Entities;
using MediatR;

namespace EnterpriseFlow.Application.Features.ProjectTasks.CreateTask;

public sealed class CreateTaskCommandHandler(IAppDbContext db) : IRequestHandler<CreateTaskCommand, Guid>
{
    public async Task<Guid> Handle(CreateTaskCommand request, CancellationToken cancellationToken)
    {
        var task = ProjectTask.Create(request.Title, request.Description, request.Priority, request.ProjectId, request.DueDate);

        db.ProjectTasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);

        return task.Id;
    }
}
