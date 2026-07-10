using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.ProjectTasks.CancelTask;

public sealed class CancelTaskCommandHandler(IAppDbContext db) : IRequestHandler<CancelTaskCommand>
{
    public async Task Handle(CancelTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await db.ProjectTasks.FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProjectTask), request.TaskId);

        task.Cancel();

        await db.SaveChangesAsync(cancellationToken);
    }
}
