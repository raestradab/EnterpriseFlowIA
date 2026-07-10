using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Workflows.CreateWorkflow;

public sealed record CreateWorkflowCommand(string Name) : IRequest<Guid>, IRequirePermission
{
    public string RequiredPermission => Permissions.Workflows.Manage;
}
