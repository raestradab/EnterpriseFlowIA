using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Infrastructure.Persistence;
using EnterpriseFlow.Infrastructure.Persistence.Interceptors;
using FluentAssertions;
using MediatR;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Api.IntegrationTests.Persistence;

/// <summary>
/// Verifies, at the persistence layer directly (no HTTP, no Application handlers — those
/// don't exist yet for Client, per Sprint 7), that <c>AppDbContext</c>'s reflection-based
/// global query filter (ADR-0003) generalizes correctly across *multiple different entity
/// types* — not just Company, which is all Sprint 4 ever proved. This is the verification the
/// Sprint 5/6 docs promised before trusting the generic mechanism for every future entity.
/// </summary>
public sealed class MultiTenantQueryFilterTests : IDisposable
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public MultiTenantQueryFilterTests() => _connection.Open();

    [Fact]
    public async Task Company_And_Client_Are_Both_Isolated_By_Tenant()
    {
        var tenantA = Guid.NewGuid();
        var tenantB = Guid.NewGuid();

        await using (var setup = CreateContext(tenantA))
        {
            await setup.Database.EnsureCreatedAsync();

            setup.Companies.Add(Company.Create("Acme Corp", null));
            setup.Clients.Add(Client.Create("Acme Client", null));
            await setup.SaveChangesAsync(CancellationToken.None);
        }

        await using var asTenantA = CreateContext(tenantA);
        (await asTenantA.Companies.CountAsync()).Should().Be(1);
        (await asTenantA.Clients.CountAsync()).Should().Be(1);

        await using var asTenantB = CreateContext(tenantB);
        (await asTenantB.Companies.CountAsync()).Should().Be(0);
        (await asTenantB.Clients.CountAsync()).Should().Be(0);
    }

    public void Dispose() => _connection.Dispose();

    private AppDbContext CreateContext(Guid tenantId)
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(_connection)
            .AddInterceptors(new AuditableEntitySaveChangesInterceptor(
                new StubCurrentTenantService(tenantId),
                new StubCurrentUserService()))
            .Options;

        return new AppDbContext(options, new StubCurrentTenantService(tenantId), new NullPublisher());
    }

    private sealed class StubCurrentTenantService(Guid tenantId) : ICurrentTenantService
    {
        public Guid TenantId { get; } = tenantId;
    }

    private sealed class StubCurrentUserService : ICurrentUserService
    {
        public Guid UserId => Guid.NewGuid();

        public IReadOnlyCollection<string> Permissions => [];

        public bool HasPermission(string permission) => true;
    }

    private sealed class NullPublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}
