using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Workflows.AddWorkflowTransition;

public sealed class AddWorkflowTransitionCommandHandler(IAppDbContext db)
    : IRequestHandler<AddWorkflowTransitionCommand, Guid>
{
    public async Task<Guid> Handle(AddWorkflowTransitionCommand request, CancellationToken cancellationToken)
    {
        var workflow = await db.WorkflowDefinitions
            .Include(w => w.States)
            .Include(w => w.Transitions)
            .FirstOrDefaultAsync(w => w.Id == request.WorkflowId, cancellationToken)
            ?? throw new NotFoundException(nameof(WorkflowDefinition), request.WorkflowId);

        var transition = workflow.AddTransition(request.Name, request.FromStateId, request.ToStateId);

        await db.SaveChangesAsync(cancellationToken);

        return transition.Id;
    }
}
