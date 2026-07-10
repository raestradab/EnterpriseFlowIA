using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Projects.AddProjectMember;

public sealed class AddProjectMemberCommandHandler(IAppDbContext db) : IRequestHandler<AddProjectMemberCommand>
{
    public async Task Handle(AddProjectMemberCommand request, CancellationToken cancellationToken)
    {
        // Members must be eager-loaded: Project.AddMember checks the in-memory collection for
        // duplicates, and an unloaded collection would silently look empty (the same class of
        // bug already hit twice in Sprint 7a's Login/RefreshAccessToken handlers).
        var project = await db.Projects
            .Include(p => p.Members)
            .FirstOrDefaultAsync(p => p.Id == request.ProjectId, cancellationToken)
            ?? throw new NotFoundException(nameof(Project), request.ProjectId);

        project.AddMember(request.UserId, request.Role);

        await db.SaveChangesAsync(cancellationToken);
    }
}
