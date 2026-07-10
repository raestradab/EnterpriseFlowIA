using EnterpriseFlow.Domain.Common;
using EnterpriseFlow.Domain.Enums;
using EnterpriseFlow.Domain.Exceptions;

namespace EnterpriseFlow.Domain.Entities;

/// <summary>
/// F3.3 (Gestión de Tareas). Named <c>ProjectTask</c>, not <c>Task</c>, to avoid clashing with
/// <see cref="System.Threading.Tasks.Task"/> throughout the codebase (see
/// <see cref="Enums.ProjectTaskStatus"/> for the same reasoning applied to its status enum).
/// A separate aggregate from <see cref="Project"/> (see that class's remarks) — the
/// "assignee must be a project member" invariant (HU-023) is enforced here by requiring the
/// membership fact as a parameter, the same pattern as <see cref="Project.Close"/>.
/// </summary>
public sealed class ProjectTask : BaseEntity, ITenantScoped, IAuditableEntity, ISoftDeletable
{
    private ProjectTask()
    {
        Title = string.Empty;
        CreatedBy = string.Empty;
    }

    public string Title { get; private set; }

    public string? Description { get; private set; }

    public TaskPriority Priority { get; private set; }

    public ProjectTaskStatus Status { get; private set; }

    public Guid ProjectId { get; private set; }

    public Guid? AssignedToUserId { get; private set; }

    public DateOnly? DueDate { get; private set; }

    public bool IsOpen => Status is ProjectTaskStatus.Todo or ProjectTaskStatus.InProgress;

    public Guid TenantId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public static ProjectTask Create(
        string title,
        string? description,
        TaskPriority priority,
        Guid projectId,
        DateOnly? dueDate)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            throw new ArgumentException("Task title is required.", nameof(title));
        }

        if (projectId == Guid.Empty)
        {
            throw new ArgumentException("Task must belong to a Project.", nameof(projectId));
        }

        return new ProjectTask
        {
            Title = title.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim(),
            Priority = priority,
            Status = ProjectTaskStatus.Todo,
            ProjectId = projectId,
            DueDate = dueDate,
        };
    }

    public void AssignTenant(Guid tenantId) => TenantId = tenantId;

    /// <summary>HU-023: rejects assignment when the user is not a member of the project team.</summary>
    public void AssignTo(Guid userId, bool isProjectMember)
    {
        if (!isProjectMember)
        {
            throw new TaskAssigneeMustBeProjectMemberException(Id, userId);
        }

        AssignedToUserId = userId;
    }

    public void Start() => Status = ProjectTaskStatus.InProgress;

    public void Complete() => Status = ProjectTaskStatus.Completed;

    public void Cancel() => Status = ProjectTaskStatus.Cancelled;

    public void MarkDeleted()
    {
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
    }
}
