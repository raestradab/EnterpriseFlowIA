using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Infrastructure.Persistence;
using EnterpriseFlow.Infrastructure.Persistence.Interceptors;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Infrastructure.SqlServerTests;

internal sealed class FixedTenantService(Guid tenantId) : ICurrentTenantService
{
    public Guid TenantId => tenantId;
}

internal sealed class FixedUserService(Guid userId) : ICurrentUserService
{
    public Guid UserId => userId;

    public IReadOnlyCollection<string> Permissions => [];

    public bool HasPermission(string permission) => true;
}

/// <summary>Domain events aren't what these tests verify — HU-102 is a pure read over history,
/// nothing here mutates in a way that needs a real handler to react.</summary>
internal sealed class NoOpPublisher : IPublisher
{
    public static readonly NoOpPublisher Instance = new();

    public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

    public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification => Task.CompletedTask;
}

/// <summary>
/// Release 4, Sprint 9 (F12.3): closes the coverage gap disclosed in r4-04-validacion.md /
/// r4-07-backend.md — SQLite (used by Api.IntegrationTests) has no Temporal Tables equivalent,
/// so HU-102 had zero automated coverage until this project. Runs against a real LocalDB
/// instance, migrated for real (same discipline as the fresh-database check in
/// r3-11-devops.md), in a database of its own so it never touches the one a developer might be
/// browsing manually via the running Api.
/// </summary>
public sealed class SqlServerFixture : IAsyncLifetime
{
    // Release 4, Sprint 11: ci.yml runs a real `mssql/server` Linux service container (no
    // LocalDB there — Windows-only) and points this at it via the environment variable; local
    // Windows dev machines fall back to LocalDB with no configuration needed, same connection
    // string this project shipped with in Sprint 9.
    public static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("SQLSERVERTESTS_CONNECTION_STRING")
        ?? "Server=(localdb)\\MSSQLLocalDB;Database=EnterpriseFlow_SqlServerTests;Trusted_Connection=True;TrustServerCertificate=True";

    public async Task InitializeAsync()
    {
        await using var context = CreateDbContext(Guid.Empty);
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public AppDbContext CreateDbContext(Guid tenantId, Guid userId = default)
    {
        var tenantService = new FixedTenantService(tenantId);
        var userService = new FixedUserService(userId);

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(ConnectionString)
            .AddInterceptors(new AuditableEntitySaveChangesInterceptor(tenantService, userService))
            .Options;

        return new AppDbContext(options, tenantService, NoOpPublisher.Instance);
    }
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SqlServer (LocalDB)";
}
