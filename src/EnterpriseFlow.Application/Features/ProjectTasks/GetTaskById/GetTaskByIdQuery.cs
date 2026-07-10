using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Enums;
using MediatR;

namespace EnterpriseFlow.Application.Features.ProjectTasks.GetTaskById;

public sealed record GetTaskByIdQuery(Guid Id) : IRequest<TaskDto?>, IRequirePermission
{
    public string RequiredPermission => Permissions.Tasks.Read;
}

public sealed record TaskDto(
    Guid Id,
    string Title,
    string? Description,
    TaskPriority Priority,
    ProjectTaskStatus Status,
    Guid ProjectId,
    Guid? AssignedToUserId,
    DateOnly? DueDate);
