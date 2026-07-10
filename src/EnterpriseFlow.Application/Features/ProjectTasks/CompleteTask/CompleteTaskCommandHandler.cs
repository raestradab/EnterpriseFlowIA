using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.ProjectTasks.CompleteTask;

public sealed class CompleteTaskCommandHandler(IAppDbContext db) : IRequestHandler<CompleteTaskCommand>
{
    public async Task Handle(CompleteTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await db.ProjectTasks.FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProjectTask), request.TaskId);

        task.Complete();

        await db.SaveChangesAsync(cancellationToken);
    }
}
