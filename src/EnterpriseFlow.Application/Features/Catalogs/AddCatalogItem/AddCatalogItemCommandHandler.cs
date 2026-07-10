using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Catalogs.AddCatalogItem;

public sealed class AddCatalogItemCommandHandler(IAppDbContext db) : IRequestHandler<AddCatalogItemCommand>
{
    public async Task Handle(AddCatalogItemCommand request, CancellationToken cancellationToken)
    {
        var catalog = await db.CatalogDefinitions
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == request.CatalogId, cancellationToken)
            ?? throw new NotFoundException(nameof(CatalogDefinition), request.CatalogId);

        catalog.AddItem(request.Key, request.Label);

        await db.SaveChangesAsync(cancellationToken);
    }
}
