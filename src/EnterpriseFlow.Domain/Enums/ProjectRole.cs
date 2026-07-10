namespace EnterpriseFlow.Domain.Enums;

/// <summary>
/// A user's role within a specific Project's team (HU-022) — scoped to the project, not a
/// global/reusable role. See <see cref="Entities.Project"/> for why "Equipos" is modeled as
/// per-project membership rather than a standalone, cross-project Team aggregate.
/// </summary>
public enum ProjectRole
{
    Developer = 0,
    QaEngineer = 1,
    Lead = 2,
    ProjectManager = 3,
}
