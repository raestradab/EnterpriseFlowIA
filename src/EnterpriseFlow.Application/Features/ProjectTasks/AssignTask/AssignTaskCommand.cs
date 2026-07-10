using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.ProjectTasks.AssignTask;

public sealed record AssignTaskCommand(Guid TaskId, Guid UserId) : IRequest, IRequirePermission
{
    public string RequiredPermission => Permissions.Tasks.Manage;
}
