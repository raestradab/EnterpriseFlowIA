using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Rag.SearchDocumentChunks;

/// <summary>ADR-0014: no dedicated vector store — <c>DocumentChunks</c> is filtered by the
/// global tenant query filter (ADR-0003) first, same as any other read, and cosine similarity is
/// computed in memory over that already-tenant-scoped set, never a cross-tenant scan.</summary>
public sealed class SearchDocumentChunksQueryHandler(IAppDbContext db, IEmbeddingClient embeddingClient)
    : IRequestHandler<SearchDocumentChunksQuery, IReadOnlyCollection<DocumentChunkSearchResultDto>>
{
    private const int TopK = 5;

    public async Task<IReadOnlyCollection<DocumentChunkSearchResultDto>> Handle(
        SearchDocumentChunksQuery request, CancellationToken cancellationToken)
    {
        var queryEmbeddings = await embeddingClient.GenerateEmbeddingsAsync([request.QueryText], cancellationToken);
        if (queryEmbeddings.Count == 0)
        {
            // No real embeddings provider configured (NullEmbeddingClient) — nothing to compare
            // against; same graceful-degradation shape as the rest of Ai:*.
            return [];
        }

        var chunks = await db.DocumentChunks
            .Select(c => new { c.DocumentId, c.Content, c.Embedding })
            .ToListAsync(cancellationToken);
        if (chunks.Count == 0)
        {
            return [];
        }

        var documentIds = chunks.Select(c => c.DocumentId).Distinct().ToList();
        var documentNames = await db.Documents
            .Where(d => documentIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, d => d.FileName, cancellationToken);

        var queryVector = queryEmbeddings[0];

        return chunks
            .Select(c => new
            {
                c.DocumentId,
                c.Content,
                Score = CosineSimilarity.Compute(queryVector, EmbeddingSerializer.ToFloats(c.Embedding)),
            })
            .OrderByDescending(c => c.Score)
            .Take(TopK)
            .Select(c => new DocumentChunkSearchResultDto(
                c.DocumentId, documentNames.GetValueOrDefault(c.DocumentId, "Documento desconocido"), c.Content, c.Score))
            .ToList();
    }
}
