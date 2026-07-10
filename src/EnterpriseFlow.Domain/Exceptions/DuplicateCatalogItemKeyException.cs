namespace EnterpriseFlow.Domain.Exceptions;

/// <summary>HU-082: two items of the same Catalog cannot share a key.</summary>
public sealed class DuplicateCatalogItemKeyException(Guid catalogDefinitionId, string key)
    : DomainException($"Catalog '{catalogDefinitionId}' already has an item with key '{key}'.")
{
}
