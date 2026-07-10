using EnterpriseFlow.Domain.Common;
using EnterpriseFlow.Domain.Enums;
using EnterpriseFlow.Domain.Events;
using EnterpriseFlow.Domain.Exceptions;

namespace EnterpriseFlow.Domain.Entities;

/// <summary>
/// F3.1 (Gestión de Proyectos), aggregate root also owning its team membership (F3.2,
/// "Equipos" scoped per-project — see <see cref="ProjectMember"/>). Deliberately does NOT
/// hold its Tasks as a child collection: <see cref="ProjectTask"/> is a separate aggregate
/// (its own lifecycle, own tenant/audit/soft-delete), so the "no open tasks" invariant for
/// <see cref="Close"/> is checked by Application (which can query across aggregates) and
/// passed in as a fact — Domain still owns the decision of whether that fact permits closing.
/// </summary>
public sealed class Project : BaseEntity, ITenantScoped, IAuditableEntity, ISoftDeletable
{
    private readonly List<ProjectMember> _members = [];

    private Project()
    {
        Name = string.Empty;
        CreatedBy = string.Empty;
    }

    public string Name { get; private set; }

    public Guid ClientId { get; private set; }

    public DateOnly? StartDate { get; private set; }

    public DateOnly? EstimatedEndDate { get; private set; }

    public ProjectStatus Status { get; private set; }

    public IReadOnlyCollection<ProjectMember> Members => _members.AsReadOnly();

    public Guid TenantId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public static Project Create(string name, Guid clientId, DateOnly? startDate, DateOnly? estimatedEndDate)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Project name is required.", nameof(name));
        }

        if (clientId == Guid.Empty)
        {
            throw new ArgumentException("Project must belong to a Client.", nameof(clientId));
        }

        if (startDate is not null && estimatedEndDate is not null && estimatedEndDate < startDate)
        {
            throw new ArgumentException("Estimated end date cannot be before the start date.", nameof(estimatedEndDate));
        }

        return new Project
        {
            Name = name.Trim(),
            ClientId = clientId,
            StartDate = startDate,
            EstimatedEndDate = estimatedEndDate,
            Status = ProjectStatus.Planned,
        };
    }

    public void AssignTenant(Guid tenantId) => TenantId = tenantId;

    /// <summary>HU-021: refuses to close while <paramref name="hasOpenTasks"/> is true.</summary>
    public void Close(bool hasOpenTasks)
    {
        if (hasOpenTasks)
        {
            throw new ProjectHasOpenTasksException(Id);
        }

        Status = ProjectStatus.Closed;
        Raise(new ProjectClosedDomainEvent(Id));
    }

    /// <summary>HU-022: adds a team member; a user cannot be added twice to the same project.</summary>
    public void AddMember(Guid userId, ProjectRole role)
    {
        if (_members.Any(m => m.UserId == userId))
        {
            throw new ProjectMemberAlreadyExistsException(Id, userId);
        }

        _members.Add(ProjectMember.Create(Id, userId, role));
    }

    public void RemoveMember(Guid userId) => _members.RemoveAll(m => m.UserId == userId);

    public bool IsMember(Guid userId) => _members.Any(m => m.UserId == userId);

    public void MarkDeleted()
    {
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
    }
}
