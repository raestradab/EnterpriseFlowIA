using System.Text;
using System.Threading.RateLimiting;
using EnterpriseFlow.Api.Authorization;
using EnterpriseFlow.Api.Endpoints;
using EnterpriseFlow.Api.Middleware;
using EnterpriseFlow.Application;
using EnterpriseFlow.Infrastructure;
using EnterpriseFlow.Infrastructure.Identity;
using EnterpriseFlow.Infrastructure.Persistence;
using EnterpriseFlow.Infrastructure.Realtime;
using Hangfire;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Single-stage Serilog setup: a two-stage bootstrap-logger (CreateBootstrapLogger() +
// ReadFrom.Services()) relies on freezing a shared, static ReloadableLogger — which breaks
// when the host is built more than once in the same process, as WebApplicationFactory-based
// integration tests do. Revisit if/when startup-time (pre-DI) logging becomes a real need.
builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration)
    .WriteTo.Console());

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Paste the access token returned by /api/auth/login.",
    });
    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
            Array.Empty<string>()
        },
    });
});
builder.Services.AddHealthChecks();

// Release 4, Sprint 10 (F4.3/ADR-0008): ADR-0008 documented this as activated in Release 2
// ("el costo de activarlo para toda la Api es prácticamente cero") but the middleware was never
// actually registered — caught auditing especificcion.md against the real Program.cs. EnableForHttps
// is normally off by default (BREACH-attack risk: compressing a response that mixes attacker-
// -controlled input with a secret can leak the secret's length/content) — safe here because this
// is a pure JSON API with no cookies/CSRF tokens ever reflected back inside a response body.
builder.Services.AddResponseCompression(options =>
{
    options.EnableForHttps = true;
    options.Providers.Add<BrotliCompressionProvider>();
    options.Providers.Add<GzipCompressionProvider>();
});

builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>()
    ?? throw new InvalidOperationException($"Missing '{JwtOptions.SectionName}' configuration section.");

// Fail fast with an actionable message instead of a bare NullReferenceException from
// Encoding.UTF8.GetBytes below. The signing key must never live in appsettings.json (it would
// ship with the repo) — configure it with `dotnet user-secrets set "Jwt:SigningKey" "<value>"`
// for local development, or an environment variable / secret manager elsewhere.
if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32)
{
    throw new InvalidOperationException(
        "Jwt:SigningKey is missing or shorter than 32 characters. Configure it via " +
        "'dotnet user-secrets set \"Jwt:SigningKey\" \"<a-long-random-value>\"' in " +
        "EnterpriseFlow.Api — it must never be stored in appsettings.json.");
}

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        // Without this, the handler's default inbound claim mapping silently rewrites "sub"
        // to the legacy ClaimTypes.NameIdentifier URI, so JwtCurrentUserService's
        // User.FindFirst(JwtRegisteredClaimNames.Sub) never finds it and UserId always comes
        // back Guid.Empty — found via manual smoke test (POST /api/auth/me returned an
        // all-zeros userId despite a correctly issued token); no automated test asserted the
        // actual UserId value, only that permissions/status codes were right.
        options.MapInboundClaims = false;

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidateAudience = true,
            ValidAudience = jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
        };

        // Release 2 (F6.1): browsers' native WebSocket API can't set an Authorization header, so
        // the SignalR JS client sends the token as a query string parameter instead — the
        // standard pattern for JWT + SignalR. Scoped to the hub path only: every other endpoint
        // still requires a real Authorization header, this doesn't relax authentication broadly.
        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                var accessToken = context.Request.Query["access_token"];
                if (!string.IsNullOrEmpty(accessToken) && context.HttpContext.Request.Path.StartsWithSegments("/hubs"))
                {
                    context.Token = accessToken;
                }

                return Task.CompletedTask;
            },
        };
    });

// ADR-0004: dynamic, permission-parametrized policies — no AddPolicy(...) call per permission.
builder.Services.AddAuthorization();
builder.Services.AddSingleton<IAuthorizationPolicyProvider, DynamicPermissionPolicyProvider>();
builder.Services.AddSingleton<IAuthorizationHandler, PermissionAuthorizationHandler>();

// Security review finding: /api/auth/* had no brute-force/enumeration throttling. Partitioned
// by client IP so one attacker can't exhaust the limiter for every other client (a global,
// unpartitioned limiter would itself become a trivial DoS vector).
//
// The "Testing" environment gets a much higher limit: WebApplicationFactory-based integration
// tests all share one TestServer "IP", so dozens of requests per test run would otherwise trip
// the same limit a real client gets, failing tests with 429 instead of the status code each
// one actually means to assert (see CustomWebApplicationFactory.UseEnvironment("Testing")).
var authRateLimit = builder.Environment.IsEnvironment("Testing") ? 10_000 : 5;

builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    options.AddPolicy("auth", httpContext => RateLimitPartition.GetFixedWindowLimiter(
        partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        factory: _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = authRateLimit,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0,
        }));
});

// Security review finding: no CORS policy existed at all. Today the Vite dev proxy makes the
// frontend same-origin, so nothing exercises this — but leaving it completely unconfigured
// invites bolting on AllowAnyOrigin() under deadline pressure the day the frontend is deployed
// to a different origin than the Api. Configured now, narrow and explicit: with no origins
// configured, the policy matches nothing (fails closed) instead of silently allowing everything.
var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? [];
builder.Services.AddCors(options => options.AddPolicy("frontend", policy =>
{
    if (allowedOrigins.Length > 0)
    {
        // AllowCredentials so the httpOnly refresh-token cookie can round-trip once frontend
        // and Api are genuinely cross-origin — CORS forbids combining this with AllowAnyOrigin,
        // which is exactly why an explicit origin allowlist is required, not a wildcard.
        policy.WithOrigins(allowedOrigins).AllowAnyHeader().AllowAnyMethod().AllowCredentials();
    }
}));

var app = builder.Build();

// Must run before any terminal middleware that writes a response directly (Swagger below is
// exactly that case: it short-circuits the pipeline for its own path without calling next(), so
// registering compression after it would silently never compress /swagger/v1/swagger.json —
// caught by testing this for real against a running instance, not just reading the docs).
app.UseResponseCompression();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseMiddleware<SecurityHeadersMiddleware>();

app.UseExceptionHandler();

// HSTS off in Development on purpose: it makes browsers remember to require HTTPS for the
// host, which is actively disruptive when the local dev profile runs over plain HTTP.
if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRateLimiter();

app.UseCors("frontend");

app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/health");
app.MapAuthEndpoints();
app.MapCompaniesEndpoints();
app.MapClientsEndpoints();
app.MapContactsEndpoints();
app.MapProjectsEndpoints();
app.MapProjectTasksEndpoints();
app.MapCatalogsEndpoints();
app.MapWorkflowsEndpoints();
app.MapDocumentsEndpoints();
app.MapNotificationsEndpoints();
app.MapAssistantEndpoints();

// F6.1 (ADR-0011): push-only Hub (see NotificationHub) — the client never calls a Hub method,
// it only listens, so no additional route/permission surface beyond "must be authenticated".
app.MapHub<NotificationHub>("/hubs/notifications").RequireAuthorization();

// Hangfire Dashboard: same reasoning as Swagger (Program.cs, above) — genuinely useful for
// exploring a portfolio project, gated to Development for the same reason. Only mapped when
// Hangfire itself is configured (ConnectionStrings:Hangfire) — see DependencyInjection.cs.
if (app.Environment.IsDevelopment() && !string.IsNullOrWhiteSpace(builder.Configuration.GetConnectionString("Hangfire")))
{
    app.MapHangfireDashboard("/hangfire");
}

// Sprint 11 (DevOps): applies pending migrations on startup so `docker-compose up` works
// against a freshly created, empty SQL Server container without a manual `dotnet ef database
// update` step. Idempotent — a no-op once the database is current. Skipped in "Testing": the
// integration test factory swaps in SQLite and creates its schema via EnsureCreated() instead
// (no migrations history table), so Migrate() there would conflict with that setup rather than
// complement it (see CustomWebApplicationFactory).
if (!app.Environment.IsEnvironment("Testing"))
{
    using var scope = app.Services.CreateScope();
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

app.Run();

// Exposed for WebApplicationFactory<Program> in EnterpriseFlow.Api.IntegrationTests.
public partial class Program
{
}
