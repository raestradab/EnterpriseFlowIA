using EnterpriseFlow.Application.Abstractions;

namespace EnterpriseFlow.Api.IntegrationTests.Fakes;

/// <summary>
/// Sprint 7b (Backend — RAG), Release 3: replaces <c>NullEmbeddingClient</c> — no real
/// OpenAI/Anthropic keys are available in this environment (r3-01-vision-y-alcance.md, sección
/// 0), but the retrieval loop (indexing → cosine similarity → grounded answer) needs a real
/// signal to prove it threads real content through, not just that it compiles. A tiny fixed
/// vocabulary stands in for real semantic embeddings — one dimension per known word, present (1)
/// or absent (0) — enough for cosine similarity to meaningfully separate "the query and this
/// chunk share a topic" from "they don't", without needing a real embedding model.
/// </summary>
public sealed class FakeEmbeddingClient : IEmbeddingClient
{
    private static readonly string[] Vocabulary =
        ["contrato", "plazo", "renovacion", "presupuesto", "vacaciones", "proyecto", "python", "garantia"];

    public Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<float[]>>(texts.Select(Embed).ToList());

    private static float[] Embed(string text)
    {
        var normalized = text.ToLowerInvariant();
        return Vocabulary.Select(word => normalized.Contains(word, StringComparison.Ordinal) ? 1f : 0f).ToArray();
    }
}
