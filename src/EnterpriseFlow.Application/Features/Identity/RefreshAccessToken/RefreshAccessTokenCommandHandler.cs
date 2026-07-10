using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Identity.Login;
using EnterpriseFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Identity.RefreshAccessToken;

/// <summary>
/// Rotation with reuse detection (HU-002, see the sequence diagram in
/// docs/03-diseno-arquitectura/04-secuencias.md): presenting a token that was already marked
/// used revokes it outright instead of silently issuing a new pair, since that can only happen
/// if the token was stolen and used by someone else first.
/// </summary>
public sealed class RefreshAccessTokenCommandHandler(IAppDbContext db, ITokenService tokenService)
    : IRequestHandler<RefreshAccessTokenCommand, LoginResult>
{
    public async Task<LoginResult> Handle(RefreshAccessTokenCommand request, CancellationToken cancellationToken)
    {
        var incomingHash = tokenService.HashRefreshToken(request.RefreshToken);

        var existing = await db.RefreshTokens.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == incomingHash, cancellationToken);

        if (existing is null)
        {
            throw new InvalidRefreshTokenException();
        }

        if (existing.IsUsed)
        {
            await RevokeDescendantChainAsync(existing, cancellationToken);
            throw new InvalidRefreshTokenException();
        }

        if (!existing.IsActive(DateTimeOffset.UtcNow))
        {
            throw new InvalidRefreshTokenException();
        }

        // RoleAssignments eager-loaded for the same reason as in LoginCommandHandler.
        var user = await db.Users.IgnoreQueryFilters()
            .Include(u => u.RoleAssignments)
            .FirstOrDefaultAsync(u => u.Id == existing.UserId && !u.IsDeleted, cancellationToken)
            ?? throw new InvalidRefreshTokenException();

        var newRawToken = tokenService.GenerateRefreshToken();
        var newToken = RefreshToken.Create(
            user.Id,
            tokenService.HashRefreshToken(newRawToken),
            DateTimeOffset.UtcNow.AddDays(30));
        newToken.AssignTenant(user.TenantId);
        db.RefreshTokens.Add(newToken);

        existing.MarkUsed(newToken.Id);

        var permissions = await PermissionResolver.ResolveAsync(db, user, cancellationToken);
        var accessToken = tokenService.GenerateAccessToken(user.Id, user.TenantId, permissions);

        await db.SaveChangesAsync(cancellationToken);

        return new LoginResult(accessToken.Value, accessToken.ExpiresAtUtc, newRawToken);
    }

    /// <summary>
    /// Security review finding: reuse detection used to revoke only the reused token itself.
    /// Since a rotated token forms a chain via <see cref="RefreshToken.ReplacedByTokenId"/>, an
    /// attacker who reused a stolen token already holds its *child* — revoking just the reused
    /// one left the attacker's active session untouched while the legitimate user got locked
    /// out instead. Walking the chain forward and revoking every descendant kills the
    /// attacker's session too, which is the actual point of reuse detection.
    /// </summary>
    private async Task RevokeDescendantChainAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        var current = token;
        current.Revoke();

        while (current.ReplacedByTokenId is { } nextId)
        {
            var next = await db.RefreshTokens.IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.Id == nextId, cancellationToken);

            if (next is null)
            {
                break;
            }

            next.Revoke();
            current = next;
        }

        await db.SaveChangesAsync(cancellationToken);
    }
}
