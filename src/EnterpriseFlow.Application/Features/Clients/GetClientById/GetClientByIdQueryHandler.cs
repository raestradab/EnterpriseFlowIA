using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Clients.GetClientById;

public sealed class GetClientByIdQueryHandler(IAppDbContext db) : IRequestHandler<GetClientByIdQuery, ClientDto?>
{
    public Task<ClientDto?> Handle(GetClientByIdQuery request, CancellationToken cancellationToken) =>
        db.Clients
            .Where(c => c.Id == request.Id)
            .Select(c => new ClientDto(c.Id, c.Name, c.CompanyId))
            .FirstOrDefaultAsync(cancellationToken);
}
