using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Clients.GetClientById;

public sealed record GetClientByIdQuery(Guid Id) : IRequest<ClientDto?>, IRequirePermission
{
    public string RequiredPermission => Permissions.Clients.Read;
}

public sealed record ClientDto(Guid Id, string Name, Guid? CompanyId);
