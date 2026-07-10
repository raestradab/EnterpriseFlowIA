using EnterpriseFlow.Application.Abstractions;
using Microsoft.AspNetCore.SignalR;

namespace EnterpriseFlow.Infrastructure.Realtime;

public sealed class SignalRNotifier(IHubContext<NotificationHub> hubContext) : IRealtimeNotifier
{
    public Task NotifyUserAsync(Guid userId, string eventName, object payload, CancellationToken cancellationToken) =>
        hubContext.Clients.User(userId.ToString()).SendAsync(eventName, payload, cancellationToken);
}
