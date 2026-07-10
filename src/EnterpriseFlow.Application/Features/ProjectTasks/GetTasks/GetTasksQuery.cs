using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Enums;
using MediatR;

namespace EnterpriseFlow.Application.Features.ProjectTasks.GetTasks;

public sealed record GetTasksQuery(Guid? ProjectId) : IRequest<IReadOnlyCollection<TaskListItemDto>>, IRequirePermission
{
    public string RequiredPermission => Permissions.Tasks.Read;
}

public sealed record TaskListItemDto(
    Guid Id,
    string Title,
    ProjectTaskStatus Status,
    TaskPriority Priority,
    Guid ProjectId,
    Guid? AssignedToUserId,
    DateOnly? DueDate);
