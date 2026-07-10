namespace EnterpriseFlow.Domain.Exceptions;

/// <summary>HU-082: editing an item that doesn't belong to the Catalog (same-aggregate lookup —
/// a cross-aggregate "not found" uses Application.Common.NotFoundException instead).</summary>
public sealed class CatalogItemNotFoundException(Guid catalogDefinitionId, Guid itemId)
    : DomainException($"Catalog '{catalogDefinitionId}' has no item '{itemId}'.")
{
}
