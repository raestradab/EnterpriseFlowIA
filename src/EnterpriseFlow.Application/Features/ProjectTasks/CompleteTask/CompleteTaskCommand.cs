using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.ProjectTasks.CompleteTask;

public sealed record CompleteTaskCommand(Guid TaskId) : IRequest, IRequirePermission
{
    public string RequiredPermission => Permissions.Tasks.Manage;
}
