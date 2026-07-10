using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Catalogs.GetCatalogs;

public sealed class GetCatalogsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetCatalogsQuery, IReadOnlyCollection<CatalogListItemDto>>
{
    public async Task<IReadOnlyCollection<CatalogListItemDto>> Handle(
        GetCatalogsQuery request,
        CancellationToken cancellationToken) =>
        await db.CatalogDefinitions
            .OrderBy(c => c.Name)
            .Select(c => new CatalogListItemDto(c.Id, c.Name, c.Items.Count))
            .ToListAsync(cancellationToken);
}
