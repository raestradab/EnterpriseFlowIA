using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.ProjectTasks.AssignTask;

/// <summary>HU-023: assignee must belong to the task's Project team (ADR-0005).</summary>
public sealed class AssignTaskCommandHandler(IAppDbContext db) : IRequestHandler<AssignTaskCommand>
{
    public async Task Handle(AssignTaskCommand request, CancellationToken cancellationToken)
    {
        var task = await db.ProjectTasks.FirstOrDefaultAsync(t => t.Id == request.TaskId, cancellationToken)
            ?? throw new NotFoundException(nameof(ProjectTask), request.TaskId);

        // Members eager-loaded: Project.IsMember reads the in-memory collection.
        var project = await db.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == task.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), task.ProjectId);

        task.AssignTo(request.UserId, project.IsMember(request.UserId));

        await db.SaveChangesAsync(cancellationToken);
    }
}
