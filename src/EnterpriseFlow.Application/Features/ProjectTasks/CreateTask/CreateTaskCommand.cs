using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Enums;
using MediatR;

namespace EnterpriseFlow.Application.Features.ProjectTasks.CreateTask;

public sealed record CreateTaskCommand(
    string Title,
    string? Description,
    TaskPriority Priority,
    Guid ProjectId,
    DateOnly? DueDate) : IRequest<Guid>, IRequirePermission
{
    public string RequiredPermission => Permissions.Tasks.Manage;
}
