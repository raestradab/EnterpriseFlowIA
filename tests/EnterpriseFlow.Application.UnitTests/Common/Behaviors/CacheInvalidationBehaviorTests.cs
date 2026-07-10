using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common.Behaviors;
using FluentAssertions;
using MediatR;
using Microsoft.Extensions.Caching.Distributed;
using Moq;

namespace EnterpriseFlow.Application.UnitTests.Common.Behaviors;

public class CacheInvalidationBehaviorTests
{
    private sealed record InvalidatingRequest : IRequest<string>, IInvalidatesCache
    {
        public IReadOnlyCollection<string> CacheKeysToInvalidate => ["my-key"];
    }

    private sealed record PlainRequest : IRequest<string>;

    [Fact]
    public async Task Handle_After_Next_Succeeds_Removes_The_Tenant_Prefixed_Key()
    {
        var tenantId = Guid.NewGuid();
        var cache = new Mock<IDistributedCache>();
        var currentTenant = new Mock<ICurrentTenantService>();
        currentTenant.SetupGet(t => t.TenantId).Returns(tenantId);

        var behavior = new CacheInvalidationBehavior<InvalidatingRequest, string>(cache.Object, currentTenant.Object);

        var result = await behavior.Handle(new InvalidatingRequest(), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
        cache.Verify(c => c.RemoveAsync($"tenant:{tenantId}:my-key", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_When_Next_Throws_Does_Not_Invalidate_Anything()
    {
        var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
        var currentTenant = new Mock<ICurrentTenantService>(MockBehavior.Strict);

        var behavior = new CacheInvalidationBehavior<InvalidatingRequest, string>(cache.Object, currentTenant.Object);

        var act = () => behavior.Handle(
            new InvalidatingRequest(),
            () => throw new InvalidOperationException("handler failed"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();

        // A failed handler never actually changed anything — invalidating would evict an entry
        // that's still accurate (ADR-0012: invalidate only after next() succeeds).
        cache.VerifyNoOtherCalls();
        currentTenant.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Handle_When_Request_Does_Not_Invalidate_Cache_Skips_The_Cache_Entirely()
    {
        var cache = new Mock<IDistributedCache>(MockBehavior.Strict);
        var currentTenant = new Mock<ICurrentTenantService>(MockBehavior.Strict);

        var behavior = new CacheInvalidationBehavior<PlainRequest, string>(cache.Object, currentTenant.Object);

        var result = await behavior.Handle(new PlainRequest(), () => Task.FromResult("ok"), CancellationToken.None);

        result.Should().Be("ok");
        cache.VerifyNoOtherCalls();
        currentTenant.VerifyNoOtherCalls();
    }
}
