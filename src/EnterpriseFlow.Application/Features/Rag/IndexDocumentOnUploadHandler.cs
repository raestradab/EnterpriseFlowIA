using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Events;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EnterpriseFlow.Application.Features.Rag;

/// <summary>
/// F10.1 (HU-100). Runs synchronously as part of the upload request, not a background job — no
/// HU or ADR in this Release calls for async indexing (see the sequence diagram in
/// 04-secuencias.md, Sprint 2), and adding a new Hangfire job type for this is more
/// infrastructure than the current scope justifies (ADR-0001).
/// </summary>
public sealed partial class IndexDocumentOnUploadHandler(
    IAppDbContext db,
    IDocumentStorageProvider storageProvider,
    IDocumentTextExtractor textExtractor,
    IEmbeddingClient embeddingClient,
    ILogger<IndexDocumentOnUploadHandler> logger)
    : INotificationHandler<DomainEventNotification<DocumentUploadedDomainEvent>>
{
    private readonly ILogger<IndexDocumentOnUploadHandler> _logger = logger;

    public async Task Handle(DomainEventNotification<DocumentUploadedDomainEvent> notification, CancellationToken cancellationToken)
    {
        try
        {
            await IndexAsync(notification.DomainEvent.DocumentId, cancellationToken);
        }
        catch (Exception ex)
        {
            // Indexing is a best-effort enhancement (HU-100: the Document is already saved
            // regardless), not part of the upload's contract — a failure here (a provider
            // outage, a file the extractor chokes on) must never fail the upload the user is
            // actually waiting on. Same resilience principle as InvokeToolAsync catching
            // ForbiddenAccessException (Sprint 4) — a real failure gets logged, not silenced.
            LogIndexingFailed(ex, notification.DomainEvent.DocumentId);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "No se pudo indexar el Documento {DocumentId} para RAG.")]
    private partial void LogIndexingFailed(Exception ex, Guid documentId);

    private async Task IndexAsync(Guid documentId, CancellationToken cancellationToken)
    {
        var document = await db.Documents.SingleOrDefaultAsync(d => d.Id == documentId, cancellationToken);
        if (document is null)
        {
            return;
        }

        await using var stream = await storageProvider.DownloadAsync(document.StorageKey, cancellationToken);
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);

        var text = textExtractor.ExtractText(document.FileName, memoryStream.ToArray());
        if (text is null)
        {
            // Unsupported extension or nothing extractable (e.g. a scanned PDF) — the Document
            // stays saved, it just doesn't participate in RAG. Expected, not an error.
            return;
        }

        var chunks = TextChunker.Split(text);
        var embeddings = await embeddingClient.GenerateEmbeddingsAsync(chunks, cancellationToken);
        if (embeddings.Count != chunks.Count)
        {
            // No real embeddings provider configured (NullEmbeddingClient always returns an
            // empty list) — nothing to persist. Also guards defensively against a real provider
            // ever returning a mismatched count, which should never happen but isn't assumed.
            return;
        }

        var existingChunks = await db.DocumentChunks.Where(c => c.DocumentId == documentId).ToListAsync(cancellationToken);
        db.DocumentChunks.RemoveRange(existingChunks);

        for (var i = 0; i < chunks.Count; i++)
        {
            var chunk = DocumentChunk.Create(documentId, i, chunks[i], EmbeddingSerializer.ToBytes(embeddings[i]));
            chunk.AssignTenant(document.TenantId);
            db.DocumentChunks.Add(chunk);
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
