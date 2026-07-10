using EnterpriseFlow.Domain.Common;

namespace EnterpriseFlow.Domain.Entities;

/// <summary>
/// Aggregate root for F2.1 (Gestión de Empresas). Chosen as the architecture validation
/// slice (Sprint 4) for being the simplest MVP entity with no cross-aggregate invariants,
/// so the proof of concept exercises the plumbing (tenancy, audit, soft delete, pipeline)
/// without coupling it to business rules that belong to Sprint 5 (Modelo de dominio).
/// </summary>
public sealed class Company : BaseEntity, ITenantScoped, IAuditableEntity, ISoftDeletable
{
    private Company()
    {
        // Required by EF Core materialization.
        Name = string.Empty;
        CreatedBy = string.Empty;
    }

    public string Name { get; private set; }

    public string? TaxId { get; private set; }

    public Guid TenantId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public static Company Create(string name, string? taxId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Company name is required.", nameof(name));
        }

        return new Company
        {
            Name = name.Trim(),
            TaxId = string.IsNullOrWhiteSpace(taxId) ? null : taxId.Trim(),
        };
    }

    public void AssignTenant(Guid tenantId) => TenantId = tenantId;

    public void MarkDeleted()
    {
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
    }
}
