using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Identity.AssignRoleToUser;

public sealed class AssignRoleToUserCommandHandler(IAppDbContext db) : IRequestHandler<AssignRoleToUserCommand>
{
    public async Task Handle(AssignRoleToUserCommand request, CancellationToken cancellationToken)
    {
        // Eager-loaded: User.AssignRole checks the in-memory collection for duplicates (same
        // reasoning as AddProjectMemberCommandHandler for Project.Members).
        var user = await db.Users.Include(u => u.RoleAssignments)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(User), request.UserId);

        var roleExists = await db.Roles.AnyAsync(r => r.Id == request.RoleId, cancellationToken);
        if (!roleExists)
        {
            throw new NotFoundException(nameof(Role), request.RoleId);
        }

        user.AssignRole(request.RoleId);
        await db.SaveChangesAsync(cancellationToken);
    }
}
