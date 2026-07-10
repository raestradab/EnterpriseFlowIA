using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.SignalR;

namespace EnterpriseFlow.Infrastructure.Realtime;

/// <summary>
/// SignalR's default <see cref="IUserIdProvider"/> reads <c>ClaimTypes.NameIdentifier</c> — but
/// <c>options.MapInboundClaims = false</c> (Program.cs, fixed in Sprint 7a after a real bug: see
/// docs/08-frontend.md) means the JWT's "sub" claim is never remapped to that URI. Without this,
/// <c>Clients.User(userId)</c> (<see cref="SignalRNotifier"/>) would never match any connection.
/// Mirrors <see cref="Identity.JwtCurrentUserService"/>'s claim lookup exactly, on purpose.
/// </summary>
public sealed class JwtSubUserIdProvider : IUserIdProvider
{
    public string? GetUserId(HubConnectionContext connection) =>
        connection.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
}
