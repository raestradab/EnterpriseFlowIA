using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Projects.GetProjectHistory;

/// <summary>ADR-0015: <c>TemporalAsOf</c> changes which physical rows the query sees (current vs.
/// a past version), not the query filters that apply to them — the global tenant filter
/// (ADR-0003) still applies exactly as it does to any other read, verified for real in
/// r4-04-validacion.md rather than assumed from EF Core's documentation alone.</summary>
public sealed class GetProjectHistoryQueryHandler(IAppDbContext db)
    : IRequestHandler<GetProjectHistoryQuery, ProjectHistoryDto?>
{
    public Task<ProjectHistoryDto?> Handle(GetProjectHistoryQuery request, CancellationToken cancellationToken) =>
        db.GetProjectsAsOf(request.AsOf)
            .Where(p => p.Id == request.ProjectId)
            .Select(p => new ProjectHistoryDto(p.Id, p.Name, p.ClientId, p.Status, request.AsOf))
            .SingleOrDefaultAsync(cancellationToken);
}
