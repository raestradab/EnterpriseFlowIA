using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;

namespace EnterpriseFlow.Application.Common.Behaviors;

/// <summary>
/// Evicts cache entries for Commands that opt in via <see cref="IInvalidatesCache"/> (ADR-0012).
/// Invalidates only <em>after</em> <c>next()</c> succeeds — a thrown exception means the handler
/// never actually changed anything, so there is nothing to invalidate.
/// </summary>
public sealed class CacheInvalidationBehavior<TRequest, TResponse>(IDistributedCache cache, ICurrentTenantService currentTenant)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        var response = await next();

        if (request is IInvalidatesCache invalidator)
        {
            foreach (var key in invalidator.CacheKeysToInvalidate)
            {
                await cache.RemoveAsync(CacheKeys.ForTenant(currentTenant.TenantId, key), cancellationToken);
            }
        }

        return response;
    }
}
