using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Catalogs;
using MediatR;

namespace EnterpriseFlow.Application.Features.Catalogs.GetCatalogItems;

/// <summary>
/// F8.2 (ADR-0012) — the one Query in Release 2 that opts into cache-aside, since it's the one
/// with the read pattern that justified activating Redis in the first place (ADR-0008): high
/// read frequency, low write frequency, on data referenced by dropdowns across the app.
/// </summary>
public sealed record GetCatalogItemsQuery(Guid CatalogId)
    : IRequest<IReadOnlyCollection<CatalogItemDto>>, IRequirePermission, ICacheableQuery
{
    public string RequiredPermission => Permissions.Catalogs.Read;

    public string CacheKey => CatalogCacheKeys.Items(CatalogId);

    public TimeSpan Ttl => TimeSpan.FromMinutes(10);
}

public sealed record CatalogItemDto(Guid Id, string Key, string Label);
