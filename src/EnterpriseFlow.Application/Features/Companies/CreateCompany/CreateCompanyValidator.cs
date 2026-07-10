using FluentValidation;

namespace EnterpriseFlow.Application.Features.Companies.CreateCompany;

public sealed class CreateCompanyValidator : AbstractValidator<CreateCompanyCommand>
{
    public CreateCompanyValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(c => c.TaxId)
            .MaximumLength(50);
    }
}
