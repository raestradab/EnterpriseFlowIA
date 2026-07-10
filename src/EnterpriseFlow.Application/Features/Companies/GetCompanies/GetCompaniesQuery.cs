using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Companies.GetCompanies;

/// <summary>
/// No pagination yet — the MVP's expected data volume per tenant doesn't need it, and adding
/// paging parameters/response envelopes now would be speculative (ADR-0001). Revisit if a real
/// tenant's list grows large enough to matter.
/// </summary>
public sealed record GetCompaniesQuery : IRequest<IReadOnlyCollection<CompanyListItemDto>>, IRequirePermission
{
    public string RequiredPermission => Permissions.Companies.Read;
}

public sealed record CompanyListItemDto(Guid Id, string Name, string? TaxId);
