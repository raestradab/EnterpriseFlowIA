using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Projects.GetProjectById;

public sealed class GetProjectByIdQueryHandler(IAppDbContext db) : IRequestHandler<GetProjectByIdQuery, ProjectDto?>
{
    public Task<ProjectDto?> Handle(GetProjectByIdQuery request, CancellationToken cancellationToken) =>
        db.Projects
            .Where(p => p.Id == request.Id)
            .Select(p => new ProjectDto(
                p.Id,
                p.Name,
                p.ClientId,
                p.StartDate,
                p.EstimatedEndDate,
                p.Status,
                p.Members.Select(m => new ProjectMemberDto(m.UserId, m.Role)).ToList()))
            .FirstOrDefaultAsync(cancellationToken);
}
