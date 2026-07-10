namespace EnterpriseFlow.Domain.Enums;

/// <summary>
/// Which aggregate a <see cref="Entities.Document"/> is attached to (F5, ADR-0009) — a
/// polymorphic reference without a physical FK, same reasoning as
/// <see cref="Entities.ProjectMember"/>.UserId (ADR-0005). Adding a fifth owner type in a
/// future Release is a new enum value plus the corresponding existence check in
/// Application, not a migration.
/// </summary>
public enum DocumentOwnerType
{
    Project = 0,
    Client = 1,
    Task = 2,
}
