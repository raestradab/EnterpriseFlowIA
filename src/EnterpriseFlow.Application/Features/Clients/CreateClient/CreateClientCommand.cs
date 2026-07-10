using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Clients.CreateClient;

public sealed record CreateClientCommand(string Name, Guid? CompanyId) : IRequest<Guid>, IRequirePermission
{
    public string RequiredPermission => Permissions.Clients.Manage;
}
