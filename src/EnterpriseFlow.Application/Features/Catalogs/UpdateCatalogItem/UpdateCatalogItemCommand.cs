using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Catalogs;
using MediatR;

namespace EnterpriseFlow.Application.Features.Catalogs.UpdateCatalogItem;

public sealed record UpdateCatalogItemCommand(Guid CatalogId, Guid ItemId, string Label)
    : IRequest, IRequirePermission, IInvalidatesCache
{
    public string RequiredPermission => Permissions.Catalogs.Manage;

    public IReadOnlyCollection<string> CacheKeysToInvalidate => [CatalogCacheKeys.Items(CatalogId)];
}
