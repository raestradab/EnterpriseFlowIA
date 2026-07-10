using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Workflows.AddWorkflowState;

public sealed class AddWorkflowStateCommandHandler(IAppDbContext db) : IRequestHandler<AddWorkflowStateCommand, Guid>
{
    public async Task<Guid> Handle(AddWorkflowStateCommand request, CancellationToken cancellationToken)
    {
        var workflow = await db.WorkflowDefinitions
            .Include(w => w.States)
            .FirstOrDefaultAsync(w => w.Id == request.WorkflowId, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkflowDefinition), request.WorkflowId);

        var state = workflow.AddState(request.Name, request.IsInitial, request.IsFinal);

        await db.SaveChangesAsync(cancellationToken);

        return state.Id;
    }
}
