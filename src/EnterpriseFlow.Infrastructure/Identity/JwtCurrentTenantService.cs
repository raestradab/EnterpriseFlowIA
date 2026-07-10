using EnterpriseFlow.Application.Abstractions;
using Microsoft.AspNetCore.Http;

namespace EnterpriseFlow.Infrastructure.Identity;

/// <summary>
/// Replaces the Sprint 4 header-based stand-in now that JWT issuance/validation exists
/// (Sprint 7a). Reads the <c>tenant_id</c> claim populated by the JWT Bearer handler from a
/// validated access token. Returns <see cref="Guid.Empty"/> — not a throw — when there is no
/// authenticated principal: anonymous flows (RegisterTenant, Login) are expected to hit this
/// and explicitly assign tenancy themselves (ADR-0006), rather than relying on an ambient value.
/// </summary>
public sealed class JwtCurrentTenantService(IHttpContextAccessor httpContextAccessor) : ICurrentTenantService
{
    public Guid TenantId
    {
        get
        {
            var claim = httpContextAccessor.HttpContext?.User.FindFirst(AppClaimTypes.TenantId)?.Value;
            return Guid.TryParse(claim, out var tenantId) ? tenantId : Guid.Empty;
        }
    }
}
