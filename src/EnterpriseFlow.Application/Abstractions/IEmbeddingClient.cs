namespace EnterpriseFlow.Application.Abstractions;

/// <summary>
/// F10.1/F10.2 (ADR-0013, ADR-0014). One active implementation per instance, selected by
/// <c>Ai:EmbeddingProvider</c> — independent from <see cref="IAiChatClient"/>'s own provider
/// selection, since not every chat provider also offers embeddings (Anthropic doesn't).
/// </summary>
public interface IEmbeddingClient
{
    /// <summary>Batched, not one call per chunk — every real embeddings API accepts multiple
    /// inputs per request, and indexing a Document produces many chunks at once (HU-100).
    /// Returns vectors in the same order as <paramref name="texts"/>.</summary>
    Task<IReadOnlyList<float[]>> GenerateEmbeddingsAsync(IReadOnlyList<string> texts, CancellationToken cancellationToken);
}
