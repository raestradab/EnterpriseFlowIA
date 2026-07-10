using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Clients.GetClients;

public sealed class GetClientsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetClientsQuery, IReadOnlyCollection<ClientListItemDto>>
{
    public async Task<IReadOnlyCollection<ClientListItemDto>> Handle(
        GetClientsQuery request,
        CancellationToken cancellationToken) =>
        await db.Clients
            .OrderBy(c => c.Name)
            .Select(c => new ClientListItemDto(c.Id, c.Name, c.CompanyId))
            .ToListAsync(cancellationToken);
}
