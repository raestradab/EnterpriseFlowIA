using EnterpriseFlow.Domain.Common;

namespace EnterpriseFlow.Domain.Entities;

/// <summary>
/// A single entry of a <see cref="CatalogDefinition"/> (F8.2). Child entity of the Catalog
/// aggregate, same shape as <see cref="RolePermission"/>/<see cref="ProjectMember"/> —
/// constructed only through <see cref="CatalogDefinition.AddItem"/>, never standalone.
/// </summary>
public sealed class CatalogItem : BaseEntity
{
    private CatalogItem()
    {
        Key = string.Empty;
        Label = string.Empty;
    }

    public Guid CatalogDefinitionId { get; private set; }

    public string Key { get; private set; }

    public string Label { get; private set; }

    internal static CatalogItem Create(Guid catalogDefinitionId, string key, string label) => new()
    {
        CatalogDefinitionId = catalogDefinitionId,
        Key = key,
        Label = label,
    };

    internal void UpdateLabel(string label) => Label = label;
}
