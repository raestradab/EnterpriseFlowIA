namespace EnterpriseFlow.Application.Abstractions;

/// <summary>
/// Pushes a real-time event to a specific user (F6.1, ADR-0011). Implemented in
/// <c>Infrastructure</c> against a SignalR <c>IHubContext</c> — <see cref="Application"/> stays
/// unaware of SignalR itself, same reasoning as <see cref="ITokenService"/> keeping JWT specifics
/// out of Application.
/// </summary>
public interface IRealtimeNotifier
{
    Task NotifyUserAsync(Guid userId, string eventName, object payload, CancellationToken cancellationToken);
}
