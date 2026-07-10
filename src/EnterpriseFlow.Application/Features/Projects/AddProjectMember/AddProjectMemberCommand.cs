using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Enums;
using MediatR;

namespace EnterpriseFlow.Application.Features.Projects.AddProjectMember;

public sealed record AddProjectMemberCommand(Guid ProjectId, Guid UserId, ProjectRole Role) : IRequest, IRequirePermission
{
    public string RequiredPermission => Permissions.Projects.Manage;
}
