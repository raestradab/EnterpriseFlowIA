namespace EnterpriseFlow.Application.Abstractions;

/// <summary>
/// Marks a Query as eligible for cache-aside via <c>CachingBehavior</c> (ADR-0012).
/// <see cref="CacheKey"/> only needs to be unique <em>within</em> the current tenant —
/// <c>CachingBehavior</c> prefixes it with the tenant id itself, so a cache shared across
/// tenants (which would break the isolation guarantee the rest of the system relies on,
/// ADR-0003) isn't something an implementer can get wrong by forgetting to include it.
/// </summary>
public interface ICacheableQuery
{
    string CacheKey { get; }

    TimeSpan Ttl { get; }
}
