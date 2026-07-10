using System.Text.Json;
using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace EnterpriseFlow.Application.Common.Behaviors;

/// <summary>
/// Cache-aside for Queries that opt in via <see cref="ICacheableQuery"/> (ADR-0012) — mirrors
/// <see cref="AuthorizationBehavior{TRequest,TResponse}"/>/<see cref="ValidationBehavior{TRequest,TResponse}"/>:
/// a Query/Handler that doesn't implement the marker is completely unaffected by this behavior.
/// </summary>
/// <remarks>
/// Found while building the first real consumer (F8.2, Sprint 4): the tenant prefix belongs
/// here, not in each <see cref="ICacheableQuery.CacheKey"/> implementation — the same
/// "infrastructure enforces isolation, not per-handler convention" reasoning ADR-0003 already
/// applies to the EF Core query filter. A Query author who forgets to fold the tenant into their
/// own key would leak data across tenants through the cache; one that can't forget doesn't.
/// </remarks>
public sealed class CachingBehavior<TRequest, TResponse>(IDistributedCache cache, ICurrentTenantService currentTenant)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (request is not ICacheableQuery cacheable)
        {
            return await next();
        }

        var key = CacheKeys.ForTenant(currentTenant.TenantId, cacheable.CacheKey);

        var cached = await cache.GetStringAsync(key, cancellationToken);
        if (cached is not null)
        {
            return JsonSerializer.Deserialize<TResponse>(cached)!;
        }

        var response = await next();

        await cache.SetStringAsync(
            key,
            JsonSerializer.Serialize(response),
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = cacheable.Ttl },
            cancellationToken);

        return response;
    }
}
