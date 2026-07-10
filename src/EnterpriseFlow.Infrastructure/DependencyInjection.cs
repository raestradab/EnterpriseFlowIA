using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Infrastructure.Ai;
using EnterpriseFlow.Infrastructure.Email;
using EnterpriseFlow.Infrastructure.Identity;
using EnterpriseFlow.Infrastructure.Persistence;
using EnterpriseFlow.Infrastructure.Persistence.Interceptors;
using EnterpriseFlow.Infrastructure.Rag;
using EnterpriseFlow.Infrastructure.Realtime;
using EnterpriseFlow.Infrastructure.Storage;
using Hangfire;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace EnterpriseFlow.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentTenantService, JwtCurrentTenantService>();
        services.AddScoped<ICurrentUserService, JwtCurrentUserService>();

        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .Validate(o => !string.IsNullOrWhiteSpace(o.SigningKey) && o.SigningKey.Length >= 32, "Jwt:SigningKey must be at least 32 characters (HMAC-SHA256 requires a sufficiently long key).")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Issuer), "Jwt:Issuer is required.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.Audience), "Jwt:Audience is required.")
            .ValidateOnStart();
        services.TryAddSingleton<ITokenService, JwtTokenService>();
        services.TryAddSingleton<IPasswordHasher, PasswordHasher>();

        services.AddScoped<AuditableEntitySaveChangesInterceptor>();

        services.AddDbContext<AppDbContext>((sp, options) => options
            .UseSqlServer(configuration.GetConnectionString("Default"))
            .AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>()));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // Release 2 (ADR-0008/ADR-0012): real Redis when configured, otherwise an in-process
        // in-memory IDistributedCache — keeps the app (and the "Testing" environment's
        // WebApplicationFactory, which never sets Redis:ConnectionString) working without a
        // reachable Redis instance. Sprint 3 wires the mechanism; no Query implements
        // ICacheableQuery yet, so nothing actually exercises this path until Sprint 4/backend.
        var redisConnectionString = configuration["Redis:ConnectionString"];
        if (!string.IsNullOrWhiteSpace(redisConnectionString))
        {
            services.AddStackExchangeRedisCache(options => options.Configuration = redisConnectionString);
        }
        else
        {
            services.AddDistributedMemoryCache();
        }

        // Release 2 (ADR-0008): background job processing for F6.2 (envío de correo asíncrono).
        // Skipped entirely (not just pointed at a fake store) when no Hangfire connection string
        // is configured — Hangfire's SQL Server storage creates its schema eagerly on startup,
        // which would fail fast against an unreachable/nonexistent database rather than degrade
        // gracefully the way the Redis fallback above does.
        var hangfireConnectionString = configuration.GetConnectionString("Hangfire");
        if (!string.IsNullOrWhiteSpace(hangfireConnectionString))
        {
            services.AddHangfire(config => config.UseSqlServerStorage(hangfireConnectionString));
            services.AddHangfireServer();

            // F6.2: only meaningful once a job queue actually exists to run it on.
            services.AddOptions<SmtpOptions>().Bind(configuration.GetSection(SmtpOptions.SectionName));
            services.AddScoped<IEmailSender, SmtpEmailSender>();
            services.AddScoped<IEmailQueue, HangfireEmailQueue>();
        }
        else
        {
            services.AddSingleton<IEmailQueue, NullEmailQueue>();
        }

        // F6.1 (ADR-0011): NotificationHub lives here (Infrastructure), mapped as an endpoint in
        // Program.cs (Api) like every other route — same split as the rest of the composition.
        services.AddSignalR();
        services.AddSingleton<IUserIdProvider, JwtSubUserIdProvider>();
        services.AddScoped<IRealtimeNotifier, SignalRNotifier>();

        // F5 (ADR-0009): exactly one IDocumentStorageProvider is ever registered — selecting a
        // provider is meant to be a configuration change (Documents:Provider), never a
        // conditional inside Application code that has to know all four exist.
        services.AddOptions<DocumentsOptions>().Bind(configuration.GetSection(DocumentsOptions.SectionName));

        // Same "Documents" section, bound again into Application's own lean options type
        // (HU-051) — Application can't reference Infrastructure.Storage.DocumentsOptions.
        services.AddOptions<DocumentValidationOptions>().Bind(configuration.GetSection(DocumentValidationOptions.SectionName));
        var documentsProvider = configuration[$"{DocumentsOptions.SectionName}:Provider"] ?? "Local";
        switch (documentsProvider.Trim().ToLowerInvariant())
        {
            case "azureblob":
                services.AddOptions<AzureBlobStorageOptions>()
                    .Bind(configuration.GetSection(AzureBlobStorageOptions.SectionName))
                    .Validate(o => !string.IsNullOrWhiteSpace(o.ConnectionString), "Documents:AzureBlob:ConnectionString is required.")
                    .ValidateOnStart();
                services.AddSingleton<IDocumentStorageProvider, AzureBlobStorageProvider>();
                break;
            case "s3":
                services.AddOptions<S3StorageOptions>()
                    .Bind(configuration.GetSection(S3StorageOptions.SectionName))
                    .Validate(o => !string.IsNullOrWhiteSpace(o.BucketName), "Documents:S3:BucketName is required.")
                    .ValidateOnStart();
                services.AddSingleton<IDocumentStorageProvider, AmazonS3StorageProvider>();
                break;
            case "gcs":
                services.AddOptions<GcsStorageOptions>()
                    .Bind(configuration.GetSection(GcsStorageOptions.SectionName))
                    .Validate(o => !string.IsNullOrWhiteSpace(o.BucketName), "Documents:Gcs:BucketName is required.")
                    .ValidateOnStart();
                services.AddSingleton<IDocumentStorageProvider, GoogleCloudStorageProvider>();
                break;
            default:
                services.AddOptions<LocalStorageOptions>().Bind(configuration.GetSection(LocalStorageOptions.SectionName));
                services.AddSingleton<IDocumentStorageProvider, LocalStorageProvider>();
                break;
        }

        // Release 3 (ADR-0013): IAiChatClient and IEmbeddingClient are selected independently
        // (Ai:ChatProvider / Ai:EmbeddingProvider) — not every chat provider also offers
        // embeddings (Anthropic doesn't), so they can't share one "Ai:Provider" switch. Both
        // default to the Null fallback when unconfigured — same graceful-degradation shape as
        // NullEmailQueue: no real API keys are available in this environment
        // (r3-01-vision-y-alcance.md, sección 0), so this DI wiring is built but not
        // runtime-verified against the real APIs (Sprint 7a).
        var chatProvider = (configuration["Ai:ChatProvider"] ?? string.Empty).Trim().ToLowerInvariant();
        var embeddingProvider = (configuration["Ai:EmbeddingProvider"] ?? string.Empty).Trim().ToLowerInvariant();

        if (chatProvider == "openai" || embeddingProvider == "openai")
        {
            services.AddOptions<OpenAiOptions>()
                .Bind(configuration.GetSection(OpenAiOptions.SectionName))
                .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "Ai:OpenAi:ApiKey is required.")
                .ValidateOnStart();
        }

        switch (chatProvider)
        {
            case "openai":
                services.AddSingleton<IAiChatClient, OpenAiChatClient>();
                break;
            case "anthropic":
                services.AddOptions<AnthropicOptions>()
                    .Bind(configuration.GetSection(AnthropicOptions.SectionName))
                    .Validate(o => !string.IsNullOrWhiteSpace(o.ApiKey), "Ai:Anthropic:ApiKey is required.")
                    .ValidateOnStart();
                services.AddHttpClient<IAiChatClient, AnthropicChatClient>();
                break;
            default:
                services.AddSingleton<IAiChatClient, NullAiChatClient>();
                break;
        }

        switch (embeddingProvider)
        {
            case "openai":
                services.AddSingleton<IEmbeddingClient, OpenAiEmbeddingClient>();
                break;
            default:
                services.AddSingleton<IEmbeddingClient, NullEmbeddingClient>();
                break;
        }

        // F10.2 (HU-100): no Null fallback — extracting text needs no external service or API
        // key, unlike the two registrations above.
        services.AddSingleton<IDocumentTextExtractor, DocumentTextExtractor>();

        // F7.5 (Release 4, ADR-0016): tracing distribuido vendor-neutral. Console por defecto —
        // spans reales, verificables en este entorno sin ninguna cuenta externa (Elastic/App
        // Insights, diferidos — r4-01-vision-y-alcance.md, sección 0); Otlp si se configura un
        // endpoint (para quien corra este stack con su propio backend compatible con OTLP).
        var otelExporter = (configuration["Observability:Exporter"] ?? "console").Trim().ToLowerInvariant();
        services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("EnterpriseFlow.Api"))
            .WithTracing(tracing =>
            {
                tracing
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddEntityFrameworkCoreInstrumentation();

                switch (otelExporter)
                {
                    case "otlp":
                        tracing.AddOtlpExporter(otlp =>
                            otlp.Endpoint = new Uri(configuration["Observability:OtlpEndpoint"] ?? "http://localhost:4317"));
                        break;
                    default:
                        tracing.AddConsoleExporter();
                        break;
                }
            });

        return services;
    }
}
