using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Domain.Entities;
using MediatR;

namespace EnterpriseFlow.Application.Features.Companies.CreateCompany;

public sealed class CreateCompanyCommandHandler(IAppDbContext db) : IRequestHandler<CreateCompanyCommand, Guid>
{
    public async Task<Guid> Handle(CreateCompanyCommand request, CancellationToken cancellationToken)
    {
        var company = Company.Create(request.Name, request.TaxId);

        db.Companies.Add(company);

        await db.SaveChangesAsync(cancellationToken);

        return company.Id;
    }
}
