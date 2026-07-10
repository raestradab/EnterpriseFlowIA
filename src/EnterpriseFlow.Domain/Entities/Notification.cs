using EnterpriseFlow.Domain.Common;

namespace EnterpriseFlow.Domain.Entities;

/// <summary>
/// F6.3 (Centro de notificaciones) — Release 2, Sprint 5. Persisted history counterpart to the
/// real-time push (<c>IRealtimeNotifier</c>, ADR-0011): a user who wasn't connected via SignalR
/// when an event fired still sees it here. No <see cref="ISoftDeletable"/> — no HU asks for
/// deleting notifications, only reading and marking them read (YAGNI, ADR-0001).
/// </summary>
public sealed class Notification : BaseEntity, ITenantScoped, IAuditableEntity
{
    private Notification()
    {
        EventName = string.Empty;
        Message = string.Empty;
        CreatedBy = string.Empty;
    }

    public Guid UserId { get; private set; }

    public string EventName { get; private set; }

    public string Message { get; private set; }

    public bool IsRead { get; private set; }

    public Guid TenantId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public static Notification Create(Guid userId, string eventName, string message)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Notification must have a recipient.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(eventName))
        {
            throw new ArgumentException("Event name is required.", nameof(eventName));
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new ArgumentException("Message is required.", nameof(message));
        }

        return new Notification
        {
            UserId = userId,
            EventName = eventName.Trim(),
            Message = message.Trim(),
        };
    }

    public void AssignTenant(Guid tenantId) => TenantId = tenantId;

    public void MarkRead() => IsRead = true;
}
