using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Catalogs.CreateCatalog;

public sealed record CreateCatalogCommand(string Name) : IRequest<Guid>, IRequirePermission
{
    public string RequiredPermission => Permissions.Catalogs.Manage;
}
