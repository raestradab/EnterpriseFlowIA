using EnterpriseFlow.Application.Common;
using FluentValidation;

namespace EnterpriseFlow.Application.Features.Identity.CreateRole;

public sealed class CreateRoleValidator : AbstractValidator<CreateRoleCommand>
{
    public CreateRoleValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleForEach(c => c.PermissionsToGrant)
            .Must(p => Permissions.All().Contains(p))
            .WithMessage("One or more permissions are not recognized.");
    }
}
