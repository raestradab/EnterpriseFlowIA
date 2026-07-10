using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Identity.Login;

public sealed class LoginCommandHandler(IAppDbContext db, IPasswordHasher passwordHasher, ITokenService tokenService)
    : IRequestHandler<LoginCommand, LoginResult>
{
    public async Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        // Bypasses the tenant filter deliberately: there is no current tenant to filter by
        // before the user is authenticated (ADR-0006). RoleAssignments must be eager-loaded —
        // there is no lazy loading configured, so PermissionResolver would otherwise see an
        // empty collection and issue a token with no permissions at all.
        var user = await db.Users.IgnoreQueryFilters()
            .Include(u => u.RoleAssignments)
            .FirstOrDefaultAsync(u => u.Email == email && !u.IsDeleted, cancellationToken);

        if (user is null)
        {
            // Timing-attack mitigation (security review finding): without this, a request for
            // a non-existent email returns almost instantly (no PBKDF2 work), while a real
            // email with a wrong password takes measurably longer — an attacker can use that
            // gap to enumerate registered emails even though the error message is identical
            // either way. Hashing here costs about the same as Verify below, closing the gap.
            passwordHasher.Hash(request.Password);
            throw new InvalidCredentialsException();
        }

        if (!passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        var permissions = await PermissionResolver.ResolveAsync(db, user, cancellationToken);
        var accessToken = tokenService.GenerateAccessToken(user.Id, user.TenantId, permissions);

        var rawRefreshToken = tokenService.GenerateRefreshToken();
        var refreshToken = RefreshToken.Create(
            user.Id,
            tokenService.HashRefreshToken(rawRefreshToken),
            DateTimeOffset.UtcNow.AddDays(30));
        refreshToken.AssignTenant(user.TenantId);
        db.RefreshTokens.Add(refreshToken);

        await db.SaveChangesAsync(cancellationToken);

        return new LoginResult(accessToken.Value, accessToken.ExpiresAtUtc, rawRefreshToken);
    }
}
