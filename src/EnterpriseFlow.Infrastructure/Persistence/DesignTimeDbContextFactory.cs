using System.Diagnostics.CodeAnalysis;
using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EnterpriseFlow.Infrastructure.Persistence;

/// <summary>
/// Lets `dotnet ef migrations add` build the model without a running DI container/HTTP
/// request — <c>ICurrentTenantService</c>/<c>IPublisher</c> are never evaluated at migration
/// time, only the shape of the query filter expression and the model itself matter.
/// </summary>
[ExcludeFromCodeCoverage(Justification = "Design-time-only tool entry point for `dotnet ef`; never executed at runtime.")]
public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=localhost,1433;Database=EnterpriseFlow;Trusted_Connection=True;TrustServerCertificate=True");

        return new AppDbContext(optionsBuilder.Options, new DesignTimeCurrentTenantService(), new DesignTimePublisher());
    }

    private sealed class DesignTimeCurrentTenantService : ICurrentTenantService
    {
        public Guid TenantId => Guid.Empty;
    }

    private sealed class DesignTimePublisher : IPublisher
    {
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}
