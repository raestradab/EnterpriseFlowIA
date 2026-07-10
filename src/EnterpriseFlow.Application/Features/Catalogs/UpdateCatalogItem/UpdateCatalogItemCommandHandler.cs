using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Catalogs.UpdateCatalogItem;

public sealed class UpdateCatalogItemCommandHandler(IAppDbContext db) : IRequestHandler<UpdateCatalogItemCommand>
{
    public async Task Handle(UpdateCatalogItemCommand request, CancellationToken cancellationToken)
    {
        var catalog = await db.CatalogDefinitions
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == request.CatalogId, cancellationToken)
            ?? throw new NotFoundException(nameof(CatalogDefinition), request.CatalogId);

        catalog.UpdateItemLabel(request.ItemId, request.Label);

        await db.SaveChangesAsync(cancellationToken);
    }
}
