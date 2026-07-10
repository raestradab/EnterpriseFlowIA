using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Infrastructure.Ai;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace EnterpriseFlow.Api.IntegrationTests;

/// <summary>
/// Sprint 9 (Pruebas), Release 3: every other test factory swaps in <c>FakeAiChatClient</c>/
/// <c>FakeEmbeddingClient</c> so the tool-use/RAG loops have a real signal to work with — but
/// that left the graceful-degradation path (no <c>Ai:ChatProvider</c>/<c>Ai:EmbeddingProvider</c>
/// configured at all, the real <c>NullAiChatClient</c>/<c>NullEmbeddingClient</c>) completely
/// untested: a fresh deployment with the Documents/Assistant features turned on before anyone
/// configures an API key is exactly the state this factory proves doesn't crash.
/// </summary>
public sealed class NullAiWebApplicationFactory : CustomWebApplicationFactory
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        base.ConfigureWebHost(builder);

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAiChatClient>();
            services.AddSingleton<IAiChatClient, NullAiChatClient>();

            services.RemoveAll<IEmbeddingClient>();
            services.AddSingleton<IEmbeddingClient, NullEmbeddingClient>();
        });
    }
}
