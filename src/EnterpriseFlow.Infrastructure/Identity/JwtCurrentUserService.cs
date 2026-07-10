using System.IdentityModel.Tokens.Jwt;
using EnterpriseFlow.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace EnterpriseFlow.Infrastructure.Identity;

/// <summary>See <see cref="JwtCurrentTenantService"/> for why this reads claims and returns
/// empty/none instead of throwing when unauthenticated.</summary>
public sealed class JwtCurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid UserId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            return Guid.TryParse(claim, out var userId) ? userId : Guid.Empty;
        }
    }

    public IReadOnlyCollection<string> Permissions =>
        httpContextAccessor.HttpContext?.User.FindAll(AppClaimTypes.Permission).Select(c => c.Value).ToList()
        ?? [];

    public bool HasPermission(string permission) => Permissions.Contains(permission);
}
