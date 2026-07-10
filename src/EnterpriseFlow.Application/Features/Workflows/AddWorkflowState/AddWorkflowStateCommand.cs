using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Workflows.AddWorkflowState;

public sealed record AddWorkflowStateCommand(Guid WorkflowId, string Name, bool IsInitial, bool IsFinal)
    : IRequest<Guid>, IRequirePermission
{
    public string RequiredPermission => Permissions.Workflows.Manage;
}
