using EnterpriseFlow.Domain.Common;
using EnterpriseFlow.Domain.Exceptions;

namespace EnterpriseFlow.Domain.Entities;

/// <summary>
/// F8.2 (Catálogos genéricos) — Release 2, Sprint 4 (validation slice). Tenant-configurable
/// reference lists (e.g. "Categorías de Documento", the consumer F5 introduces in a later
/// sprint — see r2-01-vision-y-alcance.md, section 3, on why no speculative catalog is created
/// here). Reads go through <c>GetCatalogItemsQuery</c> (<c>ICacheableQuery</c>, ADR-0012); every
/// method here that changes <see cref="Items"/> has a matching Command that implements
/// <c>IInvalidatesCache</c> so a cached read never outlives a real write.
/// </summary>
public sealed class CatalogDefinition : BaseEntity, ITenantScoped, IAuditableEntity, ISoftDeletable
{
    private readonly List<CatalogItem> _items = [];

    private CatalogDefinition()
    {
        Name = string.Empty;
        CreatedBy = string.Empty;
    }

    public string Name { get; private set; }

    public IReadOnlyCollection<CatalogItem> Items => _items.AsReadOnly();

    public Guid TenantId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public static CatalogDefinition Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Catalog name is required.", nameof(name));
        }

        return new CatalogDefinition
        {
            Name = name.Trim(),
        };
    }

    public void AssignTenant(Guid tenantId) => TenantId = tenantId;

    public void AddItem(string key, string label)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Item key is required.", nameof(key));
        }

        if (string.IsNullOrWhiteSpace(label))
        {
            throw new ArgumentException("Item label is required.", nameof(label));
        }

        if (_items.Any(i => i.Key == key))
        {
            throw new DuplicateCatalogItemKeyException(Id, key);
        }

        _items.Add(CatalogItem.Create(Id, key.Trim(), label.Trim()));
    }

    public void UpdateItemLabel(Guid itemId, string label)
    {
        var item = _items.FirstOrDefault(i => i.Id == itemId)
            ?? throw new CatalogItemNotFoundException(Id, itemId);

        item.UpdateLabel(label);
    }

    public void RemoveItem(Guid itemId) => _items.RemoveAll(i => i.Id == itemId);

    public void MarkDeleted()
    {
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
    }
}
