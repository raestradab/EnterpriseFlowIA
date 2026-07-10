using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Workflows.GetWorkflows;

public sealed class GetWorkflowsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetWorkflowsQuery, IReadOnlyCollection<WorkflowListItemDto>>
{
    public async Task<IReadOnlyCollection<WorkflowListItemDto>> Handle(
        GetWorkflowsQuery request,
        CancellationToken cancellationToken) =>
        await db.WorkflowDefinitions
            .OrderBy(w => w.Name)
            .Select(w => new WorkflowListItemDto(w.Id, w.Name, w.States.Count, w.Transitions.Count))
            .ToListAsync(cancellationToken);
}
