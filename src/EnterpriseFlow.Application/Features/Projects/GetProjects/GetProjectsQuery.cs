using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Enums;
using MediatR;

namespace EnterpriseFlow.Application.Features.Projects.GetProjects;

public sealed record GetProjectsQuery : IRequest<IReadOnlyCollection<ProjectListItemDto>>, IRequirePermission
{
    public string RequiredPermission => Permissions.Projects.Read;
}

public sealed record ProjectListItemDto(Guid Id, string Name, Guid ClientId, ProjectStatus Status);
