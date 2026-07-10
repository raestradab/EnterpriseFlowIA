using EnterpriseFlow.Application.Abstractions;
using Microsoft.Extensions.Options;
using OpenAI.Embeddings;

namespace EnterpriseFlow.Infrastructure.Ai;

/// <summary>
/// F10.1 (ADR-0013/ADR-0014), real implementation via the official <c>OpenAI</c> SDK. The only
/// concrete <see cref="IEmbeddingClient"/> in this system — Anthropic has no embeddings API, so
/// <c>Ai:EmbeddingProvider</c> only ever resolves to <c>"openai"</c> or nothing (Null fallback).
/// <b>Not runtime-verified against the real API</b> — no API key is available in this environment
/// (r3-01-vision-y-alcance.md, sección 0).
/// </summary>
public sealed class OpenAiEmbeddingClient : IEmbeddingClient
{
    private readonly EmbeddingClient _embeddingClient;

    public OpenAiEmbeddingClient(IOptions<OpenAiOptions> options)
    {
        var settings = options.Value;
        _embeddingClient = new EmbeddingClient(settings.EmbeddingModel, settings.ApiKey);
    }

    public async Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken)
    {
        if (texts.Count == 0)
        {
            return [];
        }

        var result = await _embeddingClient.GenerateEmbeddingsAsync(texts, cancellationToken: cancellationToken);

        return result.Value.Select(e => e.ToFloats().ToArray()).ToList();
    }
}
