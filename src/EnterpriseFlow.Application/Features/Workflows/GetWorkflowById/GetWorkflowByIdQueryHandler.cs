using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Workflows.GetWorkflowById;

public sealed class GetWorkflowByIdQueryHandler(IAppDbContext db) : IRequestHandler<GetWorkflowByIdQuery, WorkflowDto?>
{
    public Task<WorkflowDto?> Handle(GetWorkflowByIdQuery request, CancellationToken cancellationToken) =>
        db.WorkflowDefinitions
            .Where(w => w.Id == request.Id)
            .Select(w => new WorkflowDto(
                w.Id,
                w.Name,
                w.States.Select(s => new WorkflowStateDto(s.Id, s.Name, s.IsInitial, s.IsFinal)).ToList(),
                w.Transitions.Select(t => new WorkflowTransitionDto(t.Id, t.Name, t.FromStateId, t.ToStateId)).ToList()))
            .FirstOrDefaultAsync(cancellationToken);
}
