using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Clients.DeactivateClient;

public sealed record DeactivateClientCommand(Guid Id) : IRequest, IRequirePermission
{
    public string RequiredPermission => Permissions.Clients.Manage;
}
