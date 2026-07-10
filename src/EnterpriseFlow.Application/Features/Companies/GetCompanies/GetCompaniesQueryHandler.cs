using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Companies.GetCompanies;

public sealed class GetCompaniesQueryHandler(IAppDbContext db)
    : IRequestHandler<GetCompaniesQuery, IReadOnlyCollection<CompanyListItemDto>>
{
    public async Task<IReadOnlyCollection<CompanyListItemDto>> Handle(
        GetCompaniesQuery request,
        CancellationToken cancellationToken) =>
        await db.Companies
            .OrderBy(c => c.Name)
            .Select(c => new CompanyListItemDto(c.Id, c.Name, c.TaxId))
            .ToListAsync(cancellationToken);
}
