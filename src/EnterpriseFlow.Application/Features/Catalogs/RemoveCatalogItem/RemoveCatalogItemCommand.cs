using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Catalogs;
using MediatR;

namespace EnterpriseFlow.Application.Features.Catalogs.RemoveCatalogItem;

public sealed record RemoveCatalogItemCommand(Guid CatalogId, Guid ItemId)
    : IRequest, IRequirePermission, IInvalidatesCache
{
    public string RequiredPermission => Permissions.Catalogs.Manage;

    public IReadOnlyCollection<string> CacheKeysToInvalidate => [CatalogCacheKeys.Items(CatalogId)];
}
