using MediatR;

namespace EnterpriseFlow.Application.Features.ProjectTasks.GetMyCalendar;

/// <summary>
/// HU-024. No dedicated Domain entity — "Calendario" is a read-only projection of the current
/// user's Tasks by due date, not a business concept with invariants of its own (see
/// docs/05-modelo-dominio.md). No <c>IRequirePermission</c>: this only ever returns the
/// caller's own tasks, so there's nothing beyond authentication to authorize.
/// </summary>
public sealed record GetMyCalendarQuery(DateOnly From, DateOnly To) : IRequest<IReadOnlyCollection<CalendarItemDto>>;

public sealed record CalendarItemDto(Guid TaskId, string Title, Guid ProjectId, DateOnly DueDate);
