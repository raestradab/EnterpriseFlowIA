namespace EnterpriseFlow.Domain.Common;

/// <summary>
/// Marks an entity for automatic audit stamping (HU-040). Infrastructure's SaveChanges
/// interceptor sets these fields — domain/application code never assigns them directly.
/// </summary>
public interface IAuditableEntity
{
    DateTimeOffset CreatedAtUtc { get; }

    string CreatedBy { get; }

    DateTimeOffset? ModifiedAtUtc { get; }

    string? ModifiedBy { get; }
}
