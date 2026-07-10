using FluentValidation;

namespace EnterpriseFlow.Application.Features.Catalogs.UpdateCatalogItem;

public sealed class UpdateCatalogItemValidator : AbstractValidator<UpdateCatalogItemCommand>
{
    public UpdateCatalogItemValidator()
    {
        RuleFor(c => c.CatalogId)
            .NotEmpty();

        RuleFor(c => c.ItemId)
            .NotEmpty();

        RuleFor(c => c.Label)
            .NotEmpty()
            .MaximumLength(200);
    }
}
