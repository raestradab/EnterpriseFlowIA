using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Projects.CloseProject;

/// <summary>
/// HU-021. The "has open tasks" fact is resolved here (Application can query across
/// aggregates) and handed to <see cref="Project.Close"/>, which owns the actual decision
/// (ADR-0005) — not implemented as a separate Infrastructure Specification class as the
/// Sprint 2 component sketch (c4-03-componentes-proyectos.md) first suggested: Application
/// already has <see cref="IAppDbContext"/>, a provider-agnostic query surface, so a plain LINQ
/// predicate here achieves the same reuse (both a future "has open tasks?" UI badge and this
/// handler can call the same handler-level helper) without introducing an Infrastructure type
/// Application would need a seam to reach.
/// </summary>
public sealed class CloseProjectCommandHandler(IAppDbContext db) : IRequestHandler<CloseProjectCommand>
{
    public async Task Handle(CloseProjectCommand request, CancellationToken cancellationToken)
    {
        var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);

        var hasOpenTasks = await db.ProjectTasks
            .Where(t => t.ProjectId == request.ProjectId)
            .AnyAsync(t => t.Status == ProjectTaskStatus.Todo || t.Status == ProjectTaskStatus.InProgress, cancellationToken);

        project.Close(hasOpenTasks);

        await db.SaveChangesAsync(cancellationToken);
    }
}
