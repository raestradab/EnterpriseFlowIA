using MediatR;

namespace EnterpriseFlow.Application.Features.Notifications.GetMyNotifications;

/// <summary>HU-062. No <c>IRequirePermission</c> — this always scopes to the caller's own
/// notifications (<see cref="ICurrentUserService.UserId"/> in the handler), so there's no
/// separate permission to gate: being authenticated is the only requirement.</summary>
public sealed record GetMyNotificationsQuery : IRequest<IReadOnlyCollection<NotificationDto>>;

public sealed record NotificationDto(Guid Id, string EventName, string Message, bool IsRead, DateTimeOffset CreatedAtUtc);
