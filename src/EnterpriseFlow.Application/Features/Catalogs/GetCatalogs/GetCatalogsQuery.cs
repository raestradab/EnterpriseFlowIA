using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Catalogs.GetCatalogs;

public sealed record GetCatalogsQuery : IRequest<IReadOnlyCollection<CatalogListItemDto>>, IRequirePermission
{
    public string RequiredPermission => Permissions.Catalogs.Read;
}

public sealed record CatalogListItemDto(Guid Id, string Name, int ItemCount);
