namespace EnterpriseFlow.Application.Abstractions;

/// <summary>
/// Resolves the tenant of the current request. Implemented in Infrastructure/Api from the
/// <c>tenant_id</c> JWT claim; consumed by the EF Core global query filter (ADR-0003).
/// </summary>
public interface ICurrentTenantService
{
    Guid TenantId { get; }
}
