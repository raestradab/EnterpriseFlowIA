using System.Linq.Expressions;
using System.Reflection;
using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Common;
using EnterpriseFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Query;

namespace EnterpriseFlow.Infrastructure.Persistence;

/// <summary>
/// EF Core implementation of <see cref="IAppDbContext"/>. Every entity implementing
/// <see cref="ITenantScoped"/> and/or <see cref="ISoftDeletable"/> is enrolled automatically
/// into the corresponding global query filter (ADR-0003) via <see cref="SetGlobalQueryFilter{TEntity}"/>
/// — a new entity only needs to implement the marker interface, nothing to wire per entity.
///
/// Sprint 4 (single entity: Company) used this same reflection-based approach, hit a "reads
/// as the wrong tenant" failure, and reverted to a hand-written filter — but the actual root
/// cause (recorded in 04-validacion-arquitectura.md) was the *test* factory dropping the audit
/// interceptor registration, unrelated to this mechanism. Reinstated here for Sprint 6's 5
/// entities and re-verified end-to-end (EnterpriseFlow.Api.IntegrationTests covers 2 different
/// entity types to confirm the generic dispatch isn't type-specific).
/// </summary>
public sealed class AppDbContext(DbContextOptions<AppDbContext> options, ICurrentTenantService currentTenant, IPublisher publisher)
    : DbContext(options), IAppDbContext
{
    private static readonly MethodInfo SetGlobalQueryFilterMethodInfo = typeof(AppDbContext)
        .GetMethod(nameof(SetGlobalQueryFilter), BindingFlags.NonPublic | BindingFlags.Instance)!;

    public DbSet<Company> Companies => Set<Company>();

    public DbSet<Client> Clients => Set<Client>();

    public DbSet<Contact> Contacts => Set<Contact>();

    public DbSet<Project> Projects => Set<Project>();

    public DbSet<ProjectTask> ProjectTasks => Set<ProjectTask>();

    public DbSet<Tenant> Tenants => Set<Tenant>();

    public DbSet<User> Users => Set<User>();

    public DbSet<Role> Roles => Set<Role>();

    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

    public DbSet<CatalogDefinition> CatalogDefinitions => Set<CatalogDefinition>();

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<WorkflowDefinition> WorkflowDefinitions => Set<WorkflowDefinition>();

    public DbSet<Notification> Notifications => Set<Notification>();

    public DbSet<AssistantMessage> AssistantMessages => Set<AssistantMessage>();

    public DbSet<DocumentChunk> DocumentChunks => Set<DocumentChunk>();

    // F7.9 (HU-102, ADR-0015): TemporalAsOf is SQL Server-specific (Microsoft.EntityFrameworkCore.SqlServer),
    // which Application can't reference (ADR-0002) — this is the one place that boundary gets crossed.
    public IQueryable<Project> GetProjectsAsOf(DateTimeOffset asOf) => Projects.TemporalAsOf(asOf.UtcDateTime);

    public IQueryable<ProjectTask> GetProjectTasksAsOf(DateTimeOffset asOf) => ProjectTasks.TemporalAsOf(asOf.UtcDateTime);

    /// <summary>
    /// Publishes each changed entity's domain events, wrapped as <see cref="DomainEventNotification{TDomainEvent}"/>,
    /// only *after* the save actually succeeds — a handler reacting to
    /// <c>ClientDeactivatedDomainEvent</c> (HU-012) must never run against data that didn't
    /// actually get persisted.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var entitiesWithEvents = ChangeTracker.Entries<BaseEntity>()
            .Select(e => e.Entity)
            .Where(e => e.DomainEvents.Count > 0)
            .ToList();

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var entity in entitiesWithEvents)
        {
            var domainEvents = entity.DomainEvents.ToList();
            entity.ClearDomainEvents();

            foreach (var domainEvent in domainEvents)
            {
                var notificationType = typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType());
                var notification = (INotification)Activator.CreateInstance(notificationType, domainEvent)!;
                await publisher.Publish(notification, cancellationToken);
            }
        }

        return result;
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            var clrType = entityType.ClrType;

            if (typeof(BaseEntity).IsAssignableFrom(clrType))
            {
                // Every Id is assigned client-side (Guid.NewGuid() in BaseEntity, ADR-0002's
                // building blocks) — never by the database. Without this, EF Core's default
                // "value generated on add" convention for Guid keys makes it guess Added vs.
                // Modified from whether the key already looks non-default; for a *child* entity
                // discovered by mutating an already-tracked parent's collection (e.g.
                // Project.AddMember on a Project loaded via Include, not db.Set<T>().Add(...)),
                // that guess is wrong — it emits an UPDATE for a row that was never INSERTed,
                // which fails with a concurrency exception. Aggregates added via explicit
                // .Add(...) never hit this (Add forces State=Added outright), which is why this
                // surfaced only once a handler loaded-then-mutated a collection (AddProjectMember).
                entityType.FindProperty(nameof(BaseEntity.Id))!.ValueGenerated = ValueGenerated.Never;
            }

            if (typeof(ITenantScoped).IsAssignableFrom(clrType) || typeof(ISoftDeletable).IsAssignableFrom(clrType))
            {
                SetGlobalQueryFilterMethodInfo.MakeGenericMethod(clrType).Invoke(this, [modelBuilder]);
            }
        }
    }

    private static Expression<Func<TEntity, bool>> CombineWithAnd<TEntity>(
        Expression<Func<TEntity, bool>> left,
        Expression<Func<TEntity, bool>> right)
    {
        var parameter = Expression.Parameter(typeof(TEntity), "e");
        var leftBody = ReplacingExpressionVisitor.Replace(left.Parameters[0], parameter, left.Body);
        var rightBody = ReplacingExpressionVisitor.Replace(right.Parameters[0], parameter, right.Body);
        return Expression.Lambda<Func<TEntity, bool>>(Expression.AndAlso(leftBody, rightBody), parameter);
    }

    // Instance method: the lambdas below close over `currentTenant` (a primary-constructor
    // field on `this`), which EF Core re-binds to whichever AppDbContext instance issues the
    // query — not the instance alive when the (cached) model was built. Reflection dispatch
    // (Invoke(this, ...)) preserves this because the closure is fixed by the C# compiler at
    // this call site, not by how the method is invoked.
    private void SetGlobalQueryFilter<TEntity>(ModelBuilder modelBuilder)
        where TEntity : class
    {
        Expression<Func<TEntity, bool>>? filter = null;

        if (typeof(ITenantScoped).IsAssignableFrom(typeof(TEntity)))
        {
            Expression<Func<TEntity, bool>> tenantFilter = e => ((ITenantScoped)e).TenantId == currentTenant.TenantId;
            filter = tenantFilter;
        }

        if (typeof(ISoftDeletable).IsAssignableFrom(typeof(TEntity)))
        {
            Expression<Func<TEntity, bool>> notDeletedFilter = e => !((ISoftDeletable)e).IsDeleted;
            filter = filter is null ? notDeletedFilter : CombineWithAnd(filter, notDeletedFilter);
        }

        if (filter is not null)
        {
            modelBuilder.Entity<TEntity>().HasQueryFilter(filter);
        }
    }
}
