using EnterpriseFlow.Domain.Enums;
using MediatR;

namespace EnterpriseFlow.Application.Features.ProjectTasks.GetMyOverdueTasks;

/// <summary>
/// HU-092's Gherkin ("¿cuántas tareas tengo atrasadas?") calls for a real Query resolving
/// "overdue", not a count the model derives itself from a raw task list — deliberately not a
/// reuse of <c>GetMyCalendarQuery</c>, which has no Status filter: a completed task due
/// yesterday is not overdue, and folding that distinction into the assistant tool's caller
/// (rather than this Query) would leave every OTHER caller of GetMyCalendar with the same gap.
/// No <c>IRequirePermission</c> — same reasoning as GetMyCalendarQuery: only ever the caller's
/// own tasks.
/// </summary>
public sealed record GetMyOverdueTasksQuery : IRequest<IReadOnlyCollection<OverdueTaskDto>>;

public sealed record OverdueTaskDto(Guid Id, string Title, Guid ProjectId, DateOnly DueDate, ProjectTaskStatus Status);
