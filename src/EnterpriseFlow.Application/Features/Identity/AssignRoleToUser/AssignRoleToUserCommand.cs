using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Identity.AssignRoleToUser;

public sealed record AssignRoleToUserCommand(Guid UserId, Guid RoleId) : IRequest, IRequirePermission
{
    public string RequiredPermission => Permissions.Users.Manage;
}
