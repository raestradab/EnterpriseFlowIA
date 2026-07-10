using MediatR;

namespace EnterpriseFlow.Application.Features.Identity.GetMyPermissions;

/// <summary>
/// Backs HU-005 (Dynamic Menu) and HU-006 (Perfil): the frontend calls this once after login
/// to know which menu items/actions to render. No <c>IRequirePermission</c> — any authenticated
/// user may see their own permissions, there's nothing to authorize beyond authentication
/// itself (enforced by <c>[Authorize]</c> at the endpoint, not this pipeline).
/// </summary>
public sealed record GetMyPermissionsQuery : IRequest<MyPermissionsDto>;

public sealed record MyPermissionsDto(Guid UserId, Guid TenantId, IReadOnlyCollection<string> Permissions);
