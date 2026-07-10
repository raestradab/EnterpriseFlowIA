using FluentValidation;

namespace EnterpriseFlow.Application.Features.Catalogs.AddCatalogItem;

public sealed class AddCatalogItemValidator : AbstractValidator<AddCatalogItemCommand>
{
    public AddCatalogItemValidator()
    {
        RuleFor(c => c.CatalogId)
            .NotEmpty();

        RuleFor(c => c.Key)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(c => c.Label)
            .NotEmpty()
            .MaximumLength(200);
    }
}
