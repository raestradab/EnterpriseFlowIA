using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Clients.DeactivateClient;

public sealed class DeactivateClientCommandHandler(IAppDbContext db) : IRequestHandler<DeactivateClientCommand>
{
    public async Task Handle(DeactivateClientCommand request, CancellationToken cancellationToken)
    {
        var client = await db.Clients.FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Client), request.Id);

        client.Deactivate();

        await db.SaveChangesAsync(cancellationToken);
    }
}
