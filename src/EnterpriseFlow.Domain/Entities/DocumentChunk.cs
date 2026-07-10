using EnterpriseFlow.Domain.Common;

namespace EnterpriseFlow.Domain.Entities;

/// <summary>
/// F10.1/F10.2 (HU-100), Release 3, Sprint 5. A standalone aggregate root, not a child of
/// <see cref="Document"/> — indexing runs after upload, not in the same transaction (see the
/// sequence diagram in 04-secuencias.md), so the two don't need to change atomically together.
/// <see cref="DocumentId"/> is a cross-aggregate reference without a physical FK, same reasoning
/// ADR-0009/ADR-0005 already established for <c>Document.OwnerId</c>/<c>CurrentWorkflowStateId</c>.
/// <see cref="Embedding"/> is a serialized <c>float[]</c> (ADR-0014: a table in SQL Server, not a
/// dedicated vector store) — the actual similarity calculation used for retrieval is an
/// Application-layer concern (built in the Backend sprint), not modeled here: this entity is
/// only the storage shape, not the ranking algorithm.
/// </summary>
public sealed class DocumentChunk : BaseEntity, ITenantScoped, IAuditableEntity
{
    private DocumentChunk()
    {
        Content = string.Empty;
        Embedding = [];
        CreatedBy = string.Empty;
    }

    public Guid DocumentId { get; private set; }

    /// <summary>Gets the position of this chunk within its Document's extracted text — lets a
    /// retrieval result be traced back to "part 3 of the contract", and lets chunks be
    /// regenerated deterministically if a Document is re-indexed.</summary>
    public int ChunkIndex { get; private set; }

    public string Content { get; private set; }

    public byte[] Embedding { get; private set; }

    public Guid TenantId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public static DocumentChunk Create(Guid documentId, int chunkIndex, string content, byte[] embedding)
    {
        if (documentId == Guid.Empty)
        {
            throw new ArgumentException("Chunk must belong to a Document.", nameof(documentId));
        }

        if (chunkIndex < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(chunkIndex), chunkIndex, "Chunk index cannot be negative.");
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Content is required.", nameof(content));
        }

        if (embedding is null || embedding.Length == 0)
        {
            throw new ArgumentException("Embedding is required.", nameof(embedding));
        }

        return new DocumentChunk
        {
            DocumentId = documentId,
            ChunkIndex = chunkIndex,
            Content = content.Trim(),
            Embedding = embedding,
        };
    }

    public void AssignTenant(Guid tenantId) => TenantId = tenantId;
}
