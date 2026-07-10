namespace EnterpriseFlow.Domain.Common;

/// <summary>
/// Marks an entity as tenant-scoped. EnterpriseFlow.Infrastructure applies a global EF Core
/// query filter over every entity implementing this interface (ADR-0003) — implementing it
/// is what enrolls an entity into tenant isolation, no per-query filtering required.
/// </summary>
public interface ITenantScoped
{
    Guid TenantId { get; }

    void AssignTenant(Guid tenantId);
}
