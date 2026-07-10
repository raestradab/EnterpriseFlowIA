using EnterpriseFlow.Api.IntegrationTests.Fakes;
using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Infrastructure.Persistence;
using EnterpriseFlow.Infrastructure.Persistence.Interceptors;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EnterpriseFlow.Api.IntegrationTests;

/// <summary>
/// Swaps the real SQL Server connection for a SQLite in-memory one. SQLite (not EF Core's
/// "InMemory" provider) is used deliberately: it goes through real SQL translation, so the
/// global query filters from ADR-0003 are exercised the same way they would be against
/// SQL Server, which the InMemory provider does not guarantee.
/// Not <see langword="sealed"/> (Sprint 9, Release 3) — <see cref="NullAiWebApplicationFactory"/>
/// reuses everything here via <see langword="override"/> and only swaps the two AI fakes back to
/// the real Null* fallbacks, instead of duplicating the whole SQLite/Testing-environment setup.
/// </summary>
public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly SqliteConnection _connection = new("DataSource=:memory:");

    public CustomWebApplicationFactory()
    {
        _connection.Open();

        // Program.cs reads Jwt:SigningKey directly off WebApplicationBuilder.Configuration at
        // the very top of Main, before WebApplicationFactory's own ConfigureWebHost/
        // ConfigureAppConfiguration hooks get a chance to run — an environment variable is the
        // one override that's guaranteed to already be in place, since
        // WebApplication.CreateBuilder(args) wires up AddEnvironmentVariables() as part of
        // building the very first Configuration object. Switching to the "Testing" environment
        // below also disables ASP.NET Core's automatic user-secrets loading (only wired when
        // IsDevelopment()), which is where the real key lives for local `dotnet run` — so
        // tests need their own throwaway value regardless.
        Environment.SetEnvironmentVariable("Jwt__SigningKey", "test-only-signing-key-not-used-anywhere-else-32chars+");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Lets Program.cs relax the /api/auth/* rate limiter (see there) — tests share one
        // TestServer "IP" across dozens of requests per run, which would otherwise trip the
        // same 5-requests-per-minute limit real clients get, and start failing with 429s
        // instead of the status codes each test actually means to assert on.
        builder.UseEnvironment("Testing");

        // Keeps LocalStorageProvider (the default Documents:Provider, F5/ADR-0009) writing
        // under the OS temp dir instead of the repo's own App_Data/documents — real disk I/O
        // for a real test run, without leaving files behind in source control.
        builder.UseSetting("Documents:Local:BasePath", Path.Combine(Path.GetTempPath(), "EnterpriseFlowTests", "documents"));

        builder.ConfigureServices(services =>
        {
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>((sp, options) => options
                .UseSqlite(_connection)
                .AddInterceptors(sp.GetRequiredService<AuditableEntitySaveChangesInterceptor>()));

            // Sprint 9 (Pruebas): the real SignalRNotifier/NullEmailQueue need a connected
            // client or a configured mail queue to observe anything — replaced with recording
            // fakes so tests can assert a domain event handler actually calls them, not just
            // that a side-effect-free path through it compiles.
            services.RemoveAll<IRealtimeNotifier>();
            services.AddSingleton<FakeRealtimeNotifier>();
            services.AddSingleton<IRealtimeNotifier>(sp => sp.GetRequiredService<FakeRealtimeNotifier>());

            services.RemoveAll<IEmailQueue>();
            services.AddSingleton<FakeEmailQueue>();
            services.AddSingleton<IEmailQueue>(sp => sp.GetRequiredService<FakeEmailQueue>());

            // Sprint 4 (Validación), Release 3: no real OpenAI/Anthropic keys are available in
            // this environment — replaces NullAiChatClient with a fake that runs a real
            // two-round-trip tool-use loop, so tests prove the orchestration and the tenant/
            // permission boundary for real, not just that it compiles against a canned response.
            services.RemoveAll<IAiChatClient>();
            services.AddSingleton<IAiChatClient, FakeAiChatClient>();

            // Sprint 7b (Backend — RAG), Release 3: same reasoning as IAiChatClient above —
            // replaces NullEmbeddingClient so indexing and retrieval both have a real (if fake)
            // signal to work with end to end.
            services.RemoveAll<IEmbeddingClient>();
            services.AddSingleton<IEmbeddingClient, FakeEmbeddingClient>();

            using var scope = services.BuildServiceProvider().CreateScope();
            scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.EnsureCreated();
        });
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _connection.Dispose();
        }
    }
}
