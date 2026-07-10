using EnterpriseFlow.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Abstractions;

/// <summary>
/// Persistence seam consumed by Application handlers. Implemented by
/// Infrastructure's <c>AppDbContext</c> — Application never references EF Core's
/// concrete DbContext type directly, only this interface (ADR-0002).
/// </summary>
public interface IAppDbContext
{
    DbSet<Company> Companies { get; }

    DbSet<Client> Clients { get; }

    DbSet<Contact> Contacts { get; }

    DbSet<Project> Projects { get; }

    DbSet<ProjectTask> ProjectTasks { get; }

    DbSet<Tenant> Tenants { get; }

    DbSet<User> Users { get; }

    DbSet<Role> Roles { get; }

    DbSet<RefreshToken> RefreshTokens { get; }

    DbSet<CatalogDefinition> CatalogDefinitions { get; }

    DbSet<Document> Documents { get; }

    DbSet<WorkflowDefinition> WorkflowDefinitions { get; }

    DbSet<Notification> Notifications { get; }

    DbSet<AssistantMessage> AssistantMessages { get; }

    DbSet<DocumentChunk> DocumentChunks { get; }

    /// <summary>F7.9 (HU-102, ADR-0015). <c>Projects</c> scoped to how the row looked at
    /// <paramref name="asOf"/> — SQL Server Temporal Tables (<c>TemporalAsOf</c>) is a
    /// provider-specific EF Core feature Application can't reference directly (ADR-0002:
    /// Application depends only on the provider-agnostic <c>Microsoft.EntityFrameworkCore</c>
    /// package); this method is the seam, same reasoning as every other
    /// Infrastructure-owned capability behind an Application-defined contract.</summary>
    IQueryable<Project> GetProjectsAsOf(DateTimeOffset asOf);

    /// <summary>F7.9 (HU-102, ADR-0015) — same reasoning as <see cref="GetProjectsAsOf"/>,
    /// for the other entity HU-102 names ("un Proyecto o una Tarea").</summary>
    IQueryable<ProjectTask> GetProjectTasksAsOf(DateTimeOffset asOf);

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
