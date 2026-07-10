namespace EnterpriseFlow.Application.Common;

/// <summary>Generic "entity not found" for Application handlers. Mapped to HTTP 404.</summary>
public sealed class NotFoundException(string entityName, object key)
    : Exception($"{entityName} '{key}' was not found.")
{
}
