namespace EnterpriseFlow.Application.Common.Behaviors;

/// <summary>
/// Shared by <see cref="CachingBehavior{TRequest,TResponse}"/> and
/// <see cref="CacheInvalidationBehavior{TRequest,TResponse}"/> so the tenant-prefixing rule
/// lives in exactly one place — a cache write and its matching invalidation must always compute
/// the same key from the same inputs.
/// </summary>
internal static class CacheKeys
{
    public static string ForTenant(Guid tenantId, string key) => $"tenant:{tenantId}:{key}";
}
