using EnterpriseFlow.Application.Abstractions;

namespace EnterpriseFlow.Infrastructure.Ai;

/// <summary>
/// Sprint 3 (Arquitectura), Release 3: registered when no real provider is configured
/// (<c>Ai:EmbeddingProvider</c> unset). Returns an empty result rather than throwing — the
/// Document indexing handler (HU-100, built in the Backend sprint) already has to treat "no
/// embeddings for this text" as a normal, non-fatal outcome (a scanned PDF with no extractable
/// text layer takes the same path) — an unconfigured provider degrades to that same case instead
/// of a distinct failure mode the handler would need to special-case.
/// </summary>
public sealed class NullEmbeddingClient : IEmbeddingClient
{
    public Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<float[]>>([]);
}
