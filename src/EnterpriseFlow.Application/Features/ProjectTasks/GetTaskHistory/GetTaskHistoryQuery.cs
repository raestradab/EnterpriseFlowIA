using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Enums;
using MediatR;

namespace EnterpriseFlow.Application.Features.ProjectTasks.GetTaskHistory;

/// <summary>HU-102 (F7.9, ADR-0015) — the other entity the story names ("un Proyecto o una
/// Tarea"); see <c>GetProjectHistoryQuery</c> for the full reasoning, identical here.</summary>
public sealed record GetTaskHistoryQuery(Guid TaskId, DateTimeOffset AsOf) : IRequest<TaskHistoryDto?>, IRequirePermission
{
    public string RequiredPermission => Permissions.Tasks.Read;
}

public sealed record TaskHistoryDto(Guid Id, string Title, ProjectTaskStatus Status, Guid ProjectId, DateTimeOffset AsOf);
