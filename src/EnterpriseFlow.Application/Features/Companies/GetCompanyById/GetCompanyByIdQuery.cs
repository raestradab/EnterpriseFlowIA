using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Companies.GetCompanyById;

public sealed record GetCompanyByIdQuery(Guid Id) : IRequest<CompanyDto?>, IRequirePermission
{
    public string RequiredPermission => Permissions.Companies.Read;
}
