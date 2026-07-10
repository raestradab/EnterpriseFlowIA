using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Domain.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace EnterpriseFlow.Infrastructure.Persistence.Interceptors;

/// <summary>
/// Stamps tenant/audit fields and turns hard deletes into soft deletes, so no Application
/// handler has to remember to do it (ADR-0003 point 3, ADR-0001 point 1).
/// </summary>
public sealed class AuditableEntitySaveChangesInterceptor(
    ICurrentTenantService currentTenant,
    ICurrentUserService currentUser) : SaveChangesInterceptor
{
    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Apply(eventData.Context);
        return base.SavingChanges(eventData, result);
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData,
        InterceptionResult<int> result,
        CancellationToken cancellationToken = default)
    {
        Apply(eventData.Context);
        return base.SavingChangesAsync(eventData, result, cancellationToken);
    }

    private static void ApplySoftDelete(EntityEntry entry)
    {
        if (entry is { State: EntityState.Deleted, Entity: ISoftDeletable deletable })
        {
            deletable.MarkDeleted();
            entry.State = EntityState.Modified;
        }
    }

    private void Apply(DbContext? context)
    {
        if (context is null)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        foreach (var entry in context.ChangeTracker.Entries())
        {
            ApplyTenant(entry);
            ApplyAudit(entry, now);
            ApplySoftDelete(entry);
        }
    }

    /// <summary>
    /// Only auto-assigns when the entity doesn't already have a TenantId. RegisterTenant
    /// (ADR-0006) explicitly assigns the just-created Tenant's id to the admin Role/User before
    /// saving — there is no "current tenant" yet in that flow for this to fall back to, and it
    /// must not overwrite the explicit assignment with an empty one.
    /// </summary>
    private void ApplyTenant(EntityEntry entry)
    {
        if (entry is { State: EntityState.Added, Entity: ITenantScoped { TenantId: var tenantId } scoped }
            && tenantId == Guid.Empty)
        {
            scoped.AssignTenant(currentTenant.TenantId);
        }
    }

    private void ApplyAudit(EntityEntry entry, DateTimeOffset now)
    {
        if (entry.Entity is not IAuditableEntity)
        {
            return;
        }

        // Registration (HU-001) runs anonymously — there is no authenticated user yet to
        // attribute the audit stamp to.
        var actor = currentUser.UserId == Guid.Empty ? "system" : currentUser.UserId.ToString();

        switch (entry.State)
        {
            case EntityState.Added:
                entry.Property(nameof(IAuditableEntity.CreatedAtUtc)).CurrentValue = now;
                entry.Property(nameof(IAuditableEntity.CreatedBy)).CurrentValue = actor;
                break;
            case EntityState.Modified:
                entry.Property(nameof(IAuditableEntity.ModifiedAtUtc)).CurrentValue = now;
                entry.Property(nameof(IAuditableEntity.ModifiedBy)).CurrentValue = actor;
                break;
        }
    }
}
