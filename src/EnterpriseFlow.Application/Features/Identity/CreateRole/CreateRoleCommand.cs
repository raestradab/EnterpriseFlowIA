using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Identity.CreateRole;

public sealed record CreateRoleCommand(string Name, IReadOnlyCollection<string> PermissionsToGrant)
    : IRequest<Guid>, IRequirePermission
{
    public string RequiredPermission => Permissions.Roles.Manage;
}
