using MediatR;

namespace EnterpriseFlow.Application.Features.Identity.Logout;

/// <summary>
/// Security review finding companion: moving the refresh token to an HttpOnly cookie only
/// helps if logging out actually invalidates it server-side too — otherwise a token that's
/// merely inaccessible to JavaScript still works for anyone who gets hold of the cookie itself
/// (e.g. from disk, a backup, a shared machine) until its 30-day expiry.
/// </summary>
public sealed record LogoutCommand(string RefreshToken) : IRequest;
