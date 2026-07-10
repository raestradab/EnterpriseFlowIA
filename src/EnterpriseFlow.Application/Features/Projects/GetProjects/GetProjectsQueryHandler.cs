using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Projects.GetProjects;

public sealed class GetProjectsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetProjectsQuery, IReadOnlyCollection<ProjectListItemDto>>
{
    public async Task<IReadOnlyCollection<ProjectListItemDto>> Handle(
        GetProjectsQuery request,
        CancellationToken cancellationToken) =>
        await db.Projects
            .OrderBy(p => p.Name)
            .Select(p => new ProjectListItemDto(p.Id, p.Name, p.ClientId, p.Status))
            .ToListAsync(cancellationToken);
}
