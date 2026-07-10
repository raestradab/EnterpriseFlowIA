using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EnterpriseFlow.Infrastructure.Realtime;

/// <summary>
/// F6.1 (ADR-0011). No server-invokable methods — this Hub only ever pushes from server to
/// client (<see cref="SignalRNotifier"/>); the client never calls back into it. Mapped in
/// Program.cs (Api layer owns endpoint/hub mapping, same as every other route).
/// </summary>
[Authorize]
public sealed class NotificationHub : Hub
{
}
