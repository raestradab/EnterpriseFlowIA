using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Domain.Entities;
using MediatR;

namespace EnterpriseFlow.Application.Features.Identity.CreateRole;

public sealed class CreateRoleCommandHandler(IAppDbContext db) : IRequestHandler<CreateRoleCommand, Guid>
{
    public async Task<Guid> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
    {
        var role = Role.Create(request.Name);

        foreach (var permission in request.PermissionsToGrant.Distinct())
        {
            role.GrantPermission(permission);
        }

        db.Roles.Add(role);
        await db.SaveChangesAsync(cancellationToken);

        return role.Id;
    }
}
