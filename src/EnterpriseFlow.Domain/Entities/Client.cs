using EnterpriseFlow.Domain.Common;
using EnterpriseFlow.Domain.Events;

namespace EnterpriseFlow.Domain.Entities;

/// <summary>F2.2 (Gestión de Clientes). Optionally associated to a <see cref="Company"/>.</summary>
public sealed class Client : BaseEntity, ITenantScoped, IAuditableEntity, ISoftDeletable
{
    private Client()
    {
        Name = string.Empty;
        CreatedBy = string.Empty;
    }

    public string Name { get; private set; }

    public Guid? CompanyId { get; private set; }

    public Guid TenantId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public static Client Create(string name, Guid? companyId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Client name is required.", nameof(name));
        }

        return new Client
        {
            Name = name.Trim(),
            CompanyId = companyId,
        };
    }

    public void AssignTenant(Guid tenantId) => TenantId = tenantId;

    /// <summary>
    /// HU-012: deactivating a Client must cascade to its Contacts. Cross-aggregate, so this
    /// only raises the event — <see cref="ClientDeactivatedDomainEvent"/> — for an
    /// Application-layer handler to act on (wired in Sprint 7).
    /// </summary>
    public void Deactivate()
    {
        MarkDeleted();
        Raise(new ClientDeactivatedDomainEvent(Id));
    }

    public void MarkDeleted()
    {
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
    }
}
