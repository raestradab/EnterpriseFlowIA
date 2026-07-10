using MediatR;

namespace EnterpriseFlow.Application.Features.Identity.RegisterTenant;

/// <summary>
/// HU-001. No <c>IRequirePermission</c> — registration is necessarily anonymous, there is no
/// tenant/user context yet for a permission to be checked against.
/// </summary>
public sealed record RegisterTenantCommand(
    string TenantName,
    string TenantSlug,
    string AdminEmail,
    string AdminPassword) : IRequest<RegisterTenantResult>;

public sealed record RegisterTenantResult(Guid TenantId, Guid AdminUserId);
