namespace EnterpriseFlow.Application.Features.Catalogs;

/// <summary>
/// Shared by <c>GetCatalogItemsQuery</c> (<c>ICacheableQuery</c>) and every Command that changes
/// a Catalog's items (<c>IInvalidatesCache</c>) — a write and its matching invalidation must
/// always compute the same key, so it lives in one place instead of being duplicated per slice.
/// </summary>
internal static class CatalogCacheKeys
{
    public static string Items(Guid catalogId) => $"catalog-items:{catalogId}";
}
