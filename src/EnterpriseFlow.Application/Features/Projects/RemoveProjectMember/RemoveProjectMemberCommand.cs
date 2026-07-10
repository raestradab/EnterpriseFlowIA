using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Projects.RemoveProjectMember;

public sealed record RemoveProjectMemberCommand(Guid ProjectId, Guid UserId) : IRequest, IRequirePermission
{
    public string RequiredPermission => Permissions.Projects.Manage;
}
