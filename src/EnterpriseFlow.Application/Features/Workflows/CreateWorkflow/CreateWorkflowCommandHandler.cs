using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Domain.Entities;
using MediatR;

namespace EnterpriseFlow.Application.Features.Workflows.CreateWorkflow;

public sealed class CreateWorkflowCommandHandler(IAppDbContext db) : IRequestHandler<CreateWorkflowCommand, Guid>
{
    public async Task<Guid> Handle(CreateWorkflowCommand request, CancellationToken cancellationToken)
    {
        var workflow = WorkflowDefinition.Create(request.Name);

        db.WorkflowDefinitions.Add(workflow);

        await db.SaveChangesAsync(cancellationToken);

        return workflow.Id;
    }
}
