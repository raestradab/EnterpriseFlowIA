using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Workflows.AddWorkflowTransition;

public sealed record AddWorkflowTransitionCommand(Guid WorkflowId, string Name, Guid FromStateId, Guid ToStateId)
    : IRequest<Guid>, IRequirePermission
{
    public string RequiredPermission => Permissions.Workflows.Manage;
}
