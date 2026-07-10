using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Identity.Logout;

public sealed class LogoutCommandHandler(IAppDbContext db, ITokenService tokenService) : IRequestHandler<LogoutCommand>
{
    public async Task Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        var hash = tokenService.HashRefreshToken(request.RefreshToken);

        var token = await db.RefreshTokens.IgnoreQueryFilters()
            .FirstOrDefaultAsync(t => t.TokenHash == hash, cancellationToken);

        // Best-effort and idempotent: an unknown or already-invalid token doesn't need to
        // raise an error — the caller's intent ("end this session") is already satisfied.
        if (token is not null)
        {
            token.Revoke();
            await db.SaveChangesAsync(cancellationToken);
        }
    }
}
