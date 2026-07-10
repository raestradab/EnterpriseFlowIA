namespace EnterpriseFlow.Domain.Events;

/// <summary>
/// F10.1 (HU-100), raised by <see cref="Entities.Document.Create"/>. Carries only
/// <c>DocumentId</c> — at the moment <c>Create</c> runs, <c>TenantId</c> isn't set yet
/// (assigned separately via <c>AssignTenant</c> before <c>SaveChangesAsync</c>); by the time this
/// event dispatches (after a successful SaveChanges, same pipeline as
/// <see cref="DocumentWorkflowTransitionedDomainEvent"/>), the handler re-reads the full
/// <see cref="Entities.Document"/> row, which by then has every field populated.
/// </summary>
public sealed record DocumentUploadedDomainEvent(Guid DocumentId) : IDomainEvent
{
    public DateTimeOffset OccurredOnUtc { get; } = DateTimeOffset.UtcNow;
}
