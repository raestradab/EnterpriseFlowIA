namespace EnterpriseFlow.Application.Abstractions;

/// <summary>
/// Marks a Command whose success should evict specific cache entries via
/// <c>CacheInvalidationBehavior</c> (ADR-0012). Invalidation runs only after the handler
/// succeeds — never before, so a failed write can't evict an entry it never actually changed.
/// </summary>
public interface IInvalidatesCache
{
    IReadOnlyCollection<string> CacheKeysToInvalidate { get; }
}
