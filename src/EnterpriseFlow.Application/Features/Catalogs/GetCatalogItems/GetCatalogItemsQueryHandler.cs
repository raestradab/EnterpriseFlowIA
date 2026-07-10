using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Catalogs.GetCatalogItems;

public sealed class GetCatalogItemsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetCatalogItemsQuery, IReadOnlyCollection<CatalogItemDto>>
{
    public async Task<IReadOnlyCollection<CatalogItemDto>> Handle(
        GetCatalogItemsQuery request,
        CancellationToken cancellationToken) =>
        await db.CatalogDefinitions
            .Where(c => c.Id == request.CatalogId)
            .SelectMany(c => c.Items)
            .Select(i => new CatalogItemDto(i.Id, i.Key, i.Label))
            .ToListAsync(cancellationToken);
}
