using EnterpriseFlow.Domain.Common;
using EnterpriseFlow.Domain.Enums;

namespace EnterpriseFlow.Domain.Entities;

/// <summary>
/// F9.2/F9.3 (HU-091), Release 3. No parent "Conversation" aggregate — no HU asks for managing
/// multiple named threads, only "remember the context of my current conversation" (a single
/// implicit, ongoing thread per user within their tenant, ordered by <see cref="CreatedAtUtc"/>).
/// Mirrors <see cref="Notification"/>'s shape (a user/tenant-scoped entity with no aggregate
/// parent) rather than inventing one.
/// </summary>
public sealed class AssistantMessage : BaseEntity, ITenantScoped, IAuditableEntity
{
    private AssistantMessage()
    {
        Content = string.Empty;
        CreatedBy = string.Empty;
    }

    public Guid UserId { get; private set; }

    public AssistantMessageRole Role { get; private set; }

    public string Content { get; private set; }

    public Guid TenantId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public string CreatedBy { get; private set; }

    public DateTimeOffset? ModifiedAtUtc { get; private set; }

    public string? ModifiedBy { get; private set; }

    public static AssistantMessage Create(Guid userId, AssistantMessageRole role, string content)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Message must belong to a user.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(content))
        {
            throw new ArgumentException("Content is required.", nameof(content));
        }

        return new AssistantMessage
        {
            UserId = userId,
            Role = role,
            Content = content.Trim(),
        };
    }

    public void AssignTenant(Guid tenantId) => TenantId = tenantId;
}
