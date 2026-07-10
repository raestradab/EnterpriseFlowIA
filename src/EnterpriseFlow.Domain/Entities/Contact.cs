using EnterpriseFlow.Domain.Common;

namespace EnterpriseFlow.Domain.Entities;

/// <summary>
/// F2.3 (Gestión de Contactos). HU-012 invariant: a Contact always belongs to a Client — enforced
/// here by requiring a non-empty <paramref name="clientId"/> at creation. That the referenced
/// Client actually exists *in the same tenant* is enforced by Application, which can only load a
/// Client through the tenant-filtered <c>IAppDbContext</c> (ADR-0003) — a cross-tenant Client id
/// simply won't resolve, so Domain doesn't need to re-check tenancy itself.
/// </summary>
public sealed class Contact : BaseEntity, ITenantScoped, IAuditableEntity, ISoftDeletable
{
    private Contact()
    {
        Name = string.Empty;
        CreatedBy = string.Empty;
    }

    public string Name { get; private set; }

    public string? Email { get; private set; }

    public string? Phone { get; private set; }

    public Guid ClientId { get; private set; }

    public Guid TenantId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public static Contact Create(string name, string? email, string? phone, Guid clientId)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Contact name is required.", nameof(name));
        }

        if (clientId == Guid.Empty)
        {
            throw new ArgumentException("Contact must belong to a Client.", nameof(clientId));
        }

        return new Contact
        {
            Name = name.Trim(),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim(),
            Phone = string.IsNullOrWhiteSpace(phone) ? null : phone.Trim(),
            ClientId = clientId,
        };
    }

    public void AssignTenant(Guid tenantId) => TenantId = tenantId;

    public void MarkDeleted()
    {
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
    }
}
