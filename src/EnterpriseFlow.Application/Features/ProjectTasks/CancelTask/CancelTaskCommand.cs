using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.ProjectTasks.CancelTask;

public sealed record CancelTaskCommand(Guid TaskId) : IRequest, IRequirePermission
{
    public string RequiredPermission => Permissions.Tasks.Manage;
}
