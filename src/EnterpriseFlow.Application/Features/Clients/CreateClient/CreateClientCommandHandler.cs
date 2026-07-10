using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Domain.Entities;
using MediatR;

namespace EnterpriseFlow.Application.Features.Clients.CreateClient;

public sealed class CreateClientCommandHandler(IAppDbContext db) : IRequestHandler<CreateClientCommand, Guid>
{
    public async Task<Guid> Handle(CreateClientCommand request, CancellationToken cancellationToken)
    {
        var client = Client.Create(request.Name, request.CompanyId);

        db.Clients.Add(client);
        await db.SaveChangesAsync(cancellationToken);

        return client.Id;
    }
}
