using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Clients.GetClients;

public sealed record GetClientsQuery : IRequest<IReadOnlyCollection<ClientListItemDto>>, IRequirePermission
{
    public string RequiredPermission => Permissions.Clients.Read;
}

public sealed record ClientListItemDto(Guid Id, string Name, Guid? CompanyId);
