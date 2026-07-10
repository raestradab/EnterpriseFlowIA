using EnterpriseFlow.Api.Authorization;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Identity.AssignRoleToUser;
using EnterpriseFlow.Application.Features.Identity.CreateRole;
using EnterpriseFlow.Application.Features.Identity.GetMyPermissions;
using EnterpriseFlow.Application.Features.Identity.Login;
using EnterpriseFlow.Application.Features.Identity.Logout;
using EnterpriseFlow.Application.Features.Identity.RefreshAccessToken;
using EnterpriseFlow.Application.Features.Identity.RegisterTenant;
using MediatR;
using Microsoft.AspNetCore.RateLimiting;

namespace EnterpriseFlow.Api.Endpoints;

public static class AuthEndpoints
{
    private const string RefreshTokenCookieName = "refreshToken";

    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register-tenant", async (RegisterTenantCommand command, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                return Results.Created($"/api/auth/tenants/{result.TenantId}", result);
            })
            .RequireRateLimiting("auth")
            .WithSummary("Registers a new tenant and its admin user.")
            .WithDescription("Creates the Tenant, seeds an \"Administrator\" role with every permission in the " +
                "catalog, and creates the admin User. Anonymous. Rate-limited (5/min/IP).");

        group.MapPost("/login", async (LoginCommand command, ISender sender, HttpContext http, CancellationToken ct) =>
            {
                var result = await sender.Send(command, ct);
                SetRefreshTokenCookie(http, result.RefreshToken);
                return Results.Ok(new AccessTokenResponse(result.AccessToken, result.AccessTokenExpiresAtUtc));
            })
            .RequireRateLimiting("auth")
            .WithSummary("Authenticates and starts a session.")
            .WithDescription("Returns a short-lived access token in the body. Also sets the refresh token as an " +
                "HttpOnly cookie (never in the body — see ADR-0007); Swagger's \"Try it out\" won't show it. " +
                "Anonymous. Rate-limited (5/min/IP).");

        group.MapPost("/refresh", async (HttpContext http, ISender sender, CancellationToken ct) =>
            {
                // Security review finding: the refresh token used to travel in the request
                // body and browser localStorage — a long-lived (30 day) credential readable by
                // any JavaScript on the page. It now lives only in an HttpOnly cookie scoped to
                // /api/auth, so an XSS payload elsewhere in the app can't read it at all.
                var refreshToken = http.Request.Cookies[RefreshTokenCookieName];
                if (string.IsNullOrEmpty(refreshToken))
                {
                    return Results.Unauthorized();
                }

                var result = await sender.Send(new RefreshAccessTokenCommand(refreshToken), ct);
                SetRefreshTokenCookie(http, result.RefreshToken);
                return Results.Ok(new AccessTokenResponse(result.AccessToken, result.AccessTokenExpiresAtUtc));
            })
            .RequireRateLimiting("auth")
            .WithSummary("Rotates the refresh token cookie for a new access token.")
            .WithDescription("Takes no body — the refresh token comes from the HttpOnly cookie set by /login, so " +
                "this can't be exercised from Swagger UI directly (no cookie jar there). Single-use with reuse " +
                "detection: presenting an already-rotated token revokes its entire chain (HU-002).");

        group.MapPost("/logout", async (HttpContext http, ISender sender, CancellationToken ct) =>
        {
            var refreshToken = http.Request.Cookies[RefreshTokenCookieName];
            if (!string.IsNullOrEmpty(refreshToken))
            {
                await sender.Send(new LogoutCommand(refreshToken), ct);
            }

            ClearRefreshTokenCookie(http);
            return Results.NoContent();
        })
            .WithSummary("Ends the session.")
            .WithDescription("Revokes the refresh token server-side (not just clearing the cookie client-side) " +
                "and clears the cookie. Best-effort and idempotent — an already-invalid token is not an error.");

        group.MapGet("/me", async (ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetMyPermissionsQuery(), ct)))
            .RequireAuthorization()
            .WithSummary("Returns the caller's own identity and effective permissions.")
            .WithDescription("Resolved from the JWT's claims, not a database lookup — powers the frontend's " +
                "dynamic menu (HU-005).");

        group.MapPost("/roles", async (CreateRoleCommand command, ISender sender, CancellationToken ct) =>
            {
                var id = await sender.Send(command, ct);
                return Results.Created($"/api/auth/roles/{id}", new { id });
            })
            .RequirePermission(Permissions.Roles.Manage)
            .WithSummary("Creates a tenant-scoped role with a set of permissions.");

        group.MapPost("/users/{userId:guid}/roles/{roleId:guid}", async (
                Guid userId,
                Guid roleId,
                ISender sender,
                CancellationToken ct) =>
            {
                await sender.Send(new AssignRoleToUserCommand(userId, roleId), ct);
                return Results.NoContent();
            })
            .RequirePermission(Permissions.Users.Manage)
            .WithSummary("Grants a role to a user.");

        return app;
    }

    private static void SetRefreshTokenCookie(HttpContext http, string refreshToken)
    {
        http.Response.Cookies.Append(RefreshTokenCookieName, refreshToken, new CookieOptions
        {
            HttpOnly = true,

            // SameAsRequest, not a hardcoded true: locally (and in tests) the Api runs over
            // plain HTTP, and a Secure cookie would never be stored/sent by the client at all
            // — this adapts automatically once a real deployment terminates HTTPS in front of
            // it, without coupling this code to environment names.
            Secure = http.Request.IsHttps,

            // SameSite=Strict cookies aren't sent on cross-site requests at all, which is
            // sufficient CSRF protection for an API-only cookie with no browser-navigable effect.
            SameSite = SameSiteMode.Strict,
            Path = "/api/auth",
            Expires = DateTimeOffset.UtcNow.AddDays(30),
        });
    }

    private static void ClearRefreshTokenCookie(HttpContext http) =>
        http.Response.Cookies.Delete(RefreshTokenCookieName, new CookieOptions { Path = "/api/auth" });

    private sealed record AccessTokenResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAtUtc);
}
