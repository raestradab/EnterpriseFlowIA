using EnterpriseFlow.Application.Abstractions;
using MediatR;

namespace EnterpriseFlow.Application.Features.Identity.GetMyPermissions;

public sealed class GetMyPermissionsQueryHandler(ICurrentUserService currentUser, ICurrentTenantService currentTenant)
    : IRequestHandler<GetMyPermissionsQuery, MyPermissionsDto>
{
    public Task<MyPermissionsDto> Handle(GetMyPermissionsQuery request, CancellationToken cancellationToken) =>
        Task.FromResult(new MyPermissionsDto(currentUser.UserId, currentTenant.TenantId, currentUser.Permissions));
}
