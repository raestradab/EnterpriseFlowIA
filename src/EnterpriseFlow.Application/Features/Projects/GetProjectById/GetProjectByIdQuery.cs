using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Enums;
using MediatR;

namespace EnterpriseFlow.Application.Features.Projects.GetProjectById;

public sealed record GetProjectByIdQuery(Guid Id) : IRequest<ProjectDto?>, IRequirePermission
{
    public string RequiredPermission => Permissions.Projects.Read;
}

public sealed record ProjectMemberDto(Guid UserId, ProjectRole Role);

public sealed record ProjectDto(
    Guid Id,
    string Name,
    Guid ClientId,
    DateOnly? StartDate,
    DateOnly? EstimatedEndDate,
    ProjectStatus Status,
    IReadOnlyCollection<ProjectMemberDto> Members);
