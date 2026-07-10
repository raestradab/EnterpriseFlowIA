using MediatR;

namespace EnterpriseFlow.Application.Features.Notifications.MarkNotificationRead;

public sealed record MarkNotificationReadCommand(Guid Id) : IRequest;
