using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Companies.GetCompanyById;

public sealed class GetCompanyByIdQueryHandler(IAppDbContext db)
    : IRequestHandler<GetCompanyByIdQuery, CompanyDto?>
{
    public Task<CompanyDto?> Handle(GetCompanyByIdQuery request, CancellationToken cancellationToken) =>
        db.Companies
            .Where(c => c.Id == request.Id)
            .Select(c => new CompanyDto(c.Id, c.Name, c.TaxId))
            .FirstOrDefaultAsync(cancellationToken);
}
