using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Projects.CreateProject;

public sealed record CreateProjectCommand(
    string Name,
    Guid ClientId,
    DateOnly? StartDate,
    DateOnly? EstimatedEndDate) : IRequest<Guid>, IRequirePermission
{
    public string RequiredPermission => Permissions.Projects.Manage;
}
