namespace EnterpriseFlow.Domain.Common;

/// <summary>
/// Marks an entity as soft-deletable. Infrastructure translates hard deletes into an update
/// of these fields and applies a query filter to exclude deleted rows by default.
/// </summary>
public interface ISoftDeletable
{
    bool IsDeleted { get; }

    DateTimeOffset? DeletedAtUtc { get; }

    void MarkDeleted();
}
