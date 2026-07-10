using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Catalogs.RemoveCatalogItem;

public sealed class RemoveCatalogItemCommandHandler(IAppDbContext db) : IRequestHandler<RemoveCatalogItemCommand>
{
    public async Task Handle(RemoveCatalogItemCommand request, CancellationToken cancellationToken)
    {
        var catalog = await db.CatalogDefinitions
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == request.CatalogId, cancellationToken)
            ?? throw new NotFoundException(nameof(CatalogDefinition), request.CatalogId);

        catalog.RemoveItem(request.ItemId);

        await db.SaveChangesAsync(cancellationToken);
    }
}
