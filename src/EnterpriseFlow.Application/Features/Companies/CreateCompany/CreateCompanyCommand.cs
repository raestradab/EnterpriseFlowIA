using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Companies.CreateCompany;

public sealed record CreateCompanyCommand(string Name, string? TaxId) : IRequest<Guid>, IRequirePermission
{
    public string RequiredPermission => Permissions.Companies.Manage;
}
