using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Enums;
using MediatR;

namespace EnterpriseFlow.Application.Features.Projects.GetProjectHistory;

/// <summary>HU-102 (F7.9, ADR-0015). Same permission as reading the Project's current state —
/// seeing its history is not a lesser privilege than seeing it today.</summary>
public sealed record GetProjectHistoryQuery(Guid ProjectId, DateTimeOffset AsOf) : IRequest<ProjectHistoryDto?>, IRequirePermission
{
    public string RequiredPermission => Permissions.Projects.Read;
}

public sealed record ProjectHistoryDto(Guid Id, string Name, Guid ClientId, ProjectStatus Status, DateTimeOffset AsOf);
