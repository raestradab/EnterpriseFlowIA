using System.Text.Json;
using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common.Behaviors;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Moq;

namespace EnterpriseFlow.Application.UnitTests.Common.Behaviors;

public class CachingBehaviorTests
{
    private sealed record CacheableRequest : IRequest<string>, ICacheableQuery
    {
        public string CacheKey => "my-key";

        public TimeSpan Ttl => TimeSpan.FromMinutes(5);
    }

    private sealed record UncacheableRequest : IRequest<string>;

    [Fact]
    public async Task Handle_On_Cache_Miss_Calls_Next_And_Stores_Result_Under_A_Tenant_Prefixed_Key()
    {
        var tenantId = Guid.NewGuid();
        var cache = new Mock<IDistributedCache>();
        cache.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((byte[]?)null);
        var currentTenant = new Mock<ICurrentTenantService>();
        currentTenant.SetupGet(t => t.TenantId).Returns(tenantId);

        var behavior = new CachingBehavior<CacheableRequest, string>(cache.Object, currentTenant.Object);

        var result = await behavior.Handle(new CacheableRequest(), () => Task.FromResult("fresh-value"), CancellationToken.None);

        result.Should().Be("fresh-value");

        // Security-relevant: found while building the first real consumer (F8.2, Sprint 4) —
        // the key must be scoped to the tenant, not just the Query's own CacheKey, or a cache
        // shared across tenants could leak data (ADR-0012's "Corrección encontrada" note).
        cache.Verify(
            c => c.SetAsync(
                $"tenant:{tenantId}:my-key",
                It.IsAny<byte[]>(),
                It.IsAny<DistributedCacheEntryOptions>(),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_On_Cache_Hit_Returns_The_Cached_Value_Without_Calling_Next()
    {
        var tenantId = Guid.NewGuid();
        var cachedBytes = JsonSerializer.SerializeToUtf8Bytes("cached-value");
        var cache = new Mock<IDistributedCache>();
        cache.Setup(c => c.GetAsync($"tenant:{tenantId}:my-key", It.IsAny<CancellationToken>())).ReturnsAsync(cachedBytes);
        var currentTenant = new Mock<ICurrentTenantService>();
        currentTenant.SetupGet(t => t.TenantId).Returns(tenantId);

        var behavior = new CachingBehavior<CacheableRequest, string>(cache.Object, currentTenant.Object);
        var nextCalled = false;

        var result = await behavior.Handle(
            new CacheableRequest(),
            () =>
            {
                nextCalled = true;
                return Task.FromResult("should-not-be-used");
            },
            CancellationToken.None);

        result.Should().Be("cached-value");
        nextCalled.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_When_Request_Is_Not_Cacheable_Calls_Next_Without_Touching_The_Cache()
    {
        var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
        var currentTenant = new Mock<ICurrentTenantService>(MockBehavior.Strict);

        var behavior = new CachingBehavior<UncacheableRequest, string>(cache.Object, currentTenant.Object);

        var result = await behavior.Handle(new UncacheableRequest(), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
        cache.VerifyNoOtherCalls();
        currentTenant.VerifyNoOtherCalls();
    }
}
