using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Workflows.GetWorkflows;

public sealed record GetWorkflowsQuery : IRequest<IReadOnlyCollection<WorkflowListItemDto>>, IRequirePermission
{
    public string RequiredPermission => Permissions.Workflows.Read;
}

public sealed record WorkflowListItemDto(Guid Id, string Name, int StateCount, int TransitionCount);
