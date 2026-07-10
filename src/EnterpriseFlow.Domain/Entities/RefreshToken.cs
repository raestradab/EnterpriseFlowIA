using EnterpriseFlow.Domain.Common;

namespace EnterpriseFlow.Domain.Entities;

/// <summary>
/// HU-002: supports refresh rotation with reuse detection (see the sequence diagram in
/// docs/03-diseno-arquitectura/04-secuencias.md). Only the *hash* of the token is stored —
/// same reasoning as <see cref="User.PasswordHash"/>: a leaked database dump must not hand out
/// usable tokens. Tenant-scoped for defense in depth, even though a token is only ever looked
/// up by its own hash.
/// </summary>
public sealed class RefreshToken : BaseEntity, ITenantScoped
{
    private RefreshToken()
    {
        TokenHash = string.Empty;
    }

    public Guid UserId { get; private set; }

    public string TokenHash { get; private set; }

    public DateTimeOffset ExpiresAtUtc { get; private set; }

    public bool IsUsed { get; private set; }

    public bool IsRevoked { get; private set; }

    public Guid? ReplacedByTokenId { get; private set; }

    public Guid TenantId { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static RefreshToken Create(Guid userId, string tokenHash, DateTimeOffset expiresAtUtc)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("Refresh token must belong to a User.", nameof(userId));
        }

        if (string.IsNullOrWhiteSpace(tokenHash))
        {
            throw new ArgumentException("Token hash is required.", nameof(tokenHash));
        }

        return new RefreshToken
        {
            UserId = userId,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = DateTimeOffset.UtcNow,
        };
    }

    public void AssignTenant(Guid tenantId) => TenantId = tenantId;

    public bool IsActive(DateTimeOffset now) => !IsUsed && !IsRevoked && now < ExpiresAtUtc;

    public void MarkUsed(Guid replacedByTokenId)
    {
        IsUsed = true;
        ReplacedByTokenId = replacedByTokenId;
    }

    public void Revoke() => IsRevoked = true;
}
