using FluentValidation;

namespace EnterpriseFlow.Application.Features.Catalogs.CreateCatalog;

public sealed class CreateCatalogValidator : AbstractValidator<CreateCatalogCommand>
{
    public CreateCatalogValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
