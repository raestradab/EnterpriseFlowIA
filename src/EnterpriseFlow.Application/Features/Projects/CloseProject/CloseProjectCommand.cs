using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Projects.CloseProject;

public sealed record CloseProjectCommand(Guid ProjectId) : IRequest, IRequirePermission
{
    public string RequiredPermission => Permissions.Projects.Manage;
}
