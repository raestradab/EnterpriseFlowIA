namespace EnterpriseFlow.Api.Middleware;

/// <summary>
/// Baseline security headers (security review finding — the spec's SEGURIDAD section
/// requires them explicitly and none were set). This is a JSON API, not an HTML site, so the
/// CSP is intentionally strict (<c>default-src 'self'</c>) — the one HTML surface it also
/// covers is Swagger UI, which only loads in Development anyway.
/// </summary>
public sealed class SecurityHeadersMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var headers = context.Response.Headers;
        headers.Append("X-Content-Type-Options", "nosniff");
        headers.Append("X-Frame-Options", "DENY");
        headers.Append("Referrer-Policy", "no-referrer");
        headers.Append("Permissions-Policy", "geolocation=(), camera=(), microphone=()");
        headers.Append("Content-Security-Policy", "default-src 'self'; frame-ancestors 'none'");

        await next(context);
    }
}
