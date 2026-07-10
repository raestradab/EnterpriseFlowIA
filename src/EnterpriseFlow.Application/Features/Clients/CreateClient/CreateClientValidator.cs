using EnterpriseFlow.Application.Abstractions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Clients.CreateClient;

public sealed class CreateClientValidator : AbstractValidator<CreateClientCommand>
{
    public CreateClientValidator(IAppDbContext db)
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .MaximumLength(200);

        // The Company lookup goes through the tenant-filtered DbSet (ADR-0003), so a
        // cross-tenant CompanyId simply won't be found — no separate tenant check needed here.
        RuleFor(c => c.CompanyId)
            .MustAsync(async (companyId, ct) => companyId is null || await db.Companies.AnyAsync(c => c.Id == companyId, ct))
            .WithMessage("The specified Company does not exist.");
    }
}
