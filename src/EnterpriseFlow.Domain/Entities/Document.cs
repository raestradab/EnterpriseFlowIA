using EnterpriseFlow.Domain.Common;
using EnterpriseFlow.Domain.Enums;
using EnterpriseFlow.Domain.Events;
using EnterpriseFlow.Domain.Exceptions;

namespace EnterpriseFlow.Domain.Entities;

/// <summary>
/// F5 (Documentos), Release 2 Sprint 5. See ADR-0009 for why the owner reference is polymorphic
/// (<see cref="OwnerType"/>/<see cref="OwnerId"/>, no physical FK — same pattern as
/// <see cref="ProjectMember"/>) and why <see cref="StorageKey"/> is opaque (never a provider
/// URL or a local file path). Every Document enters a Workflow (HU-081, ADR-0010) at creation —
/// there is no "documentless of a workflow" state, unlike Catalogs which have no such concept.
/// </summary>
public sealed class Document : BaseEntity, ITenantScoped, IAuditableEntity, ISoftDeletable
{
    private Document()
    {
        FileName = string.Empty;
        ContentType = string.Empty;
        StorageKey = string.Empty;
        CreatedBy = string.Empty;
    }

    public string FileName { get; private set; }

    public string ContentType { get; private set; }

    public long SizeBytes { get; private set; }

    public DocumentOwnerType OwnerType { get; private set; }

    public Guid OwnerId { get; private set; }

    public string StorageKey { get; private set; }

    public Guid CurrentWorkflowStateId { get; private set; }

    public Guid TenantId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public DateTimeOffset? DeletedAtUtc { get; private set; }

    public static Document Create(
        string fileName,
        string contentType,
        long sizeBytes,
        DocumentOwnerType ownerType,
        Guid ownerId,
        string storageKey,
        Guid initialWorkflowStateId)
    {
        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentException("File name is required.", nameof(fileName));
        }

        if (string.IsNullOrWhiteSpace(contentType))
        {
            throw new ArgumentException("Content type is required.", nameof(contentType));
        }

        if (sizeBytes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sizeBytes), sizeBytes, "File size must be greater than zero.");
        }

        if (ownerId == Guid.Empty)
        {
            throw new ArgumentException("Document must belong to an owner.", nameof(ownerId));
        }

        if (string.IsNullOrWhiteSpace(storageKey))
        {
            throw new ArgumentException("Storage key is required.", nameof(storageKey));
        }

        if (initialWorkflowStateId == Guid.Empty)
        {
            throw new ArgumentException("Document must start in a valid workflow state.", nameof(initialWorkflowStateId));
        }

        var document = new Document
        {
            FileName = fileName.Trim(),
            ContentType = contentType.Trim(),
            SizeBytes = sizeBytes,
            OwnerType = ownerType,
            OwnerId = ownerId,
            StorageKey = storageKey,
            CurrentWorkflowStateId = initialWorkflowStateId,
        };

        document.Raise(new DocumentUploadedDomainEvent(document.Id));

        return document;
    }

    public void AssignTenant(Guid tenantId) => TenantId = tenantId;

    /// <summary>HU-080/HU-081: <paramref name="isTransitionAllowed"/> and
    /// <paramref name="targetStateName"/> are facts Application resolved by consulting the
    /// owning WorkflowDefinition — same "hecho inyectado" pattern as
    /// <see cref="Project.Close"/> (ADR-0005/ADR-0010).</summary>
    public void TransitionTo(Guid targetStateId, string targetStateName, bool isTransitionAllowed)
    {
        if (!isTransitionAllowed)
        {
            throw new InvalidWorkflowTransitionException(Id, targetStateId);
        }

        CurrentWorkflowStateId = targetStateId;
        Raise(new DocumentWorkflowTransitionedDomainEvent(Id, targetStateId, targetStateName, OwnerType, OwnerId));
    }

    public void MarkDeleted()
    {
        IsDeleted = true;
        DeletedAtUtc = DateTimeOffset.UtcNow;
    }
}
