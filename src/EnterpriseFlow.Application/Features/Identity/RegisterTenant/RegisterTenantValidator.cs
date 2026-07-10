using FluentValidation;

namespace EnterpriseFlow.Application.Features.Identity.RegisterTenant;

public sealed class RegisterTenantValidator : AbstractValidator<RegisterTenantCommand>
{
    public RegisterTenantValidator()
    {
        RuleFor(c => c.TenantName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(c => c.TenantSlug)
            .NotEmpty()
            .MaximumLength(100)
            .Matches("^[a-z0-9-]+$")
            .WithMessage("Slug must contain only lowercase letters, digits, and hyphens.");

        RuleFor(c => c.AdminEmail)
            .NotEmpty()
            .EmailAddress()
            .MaximumLength(256);

        RuleFor(c => c.AdminPassword)
            .NotEmpty()
            .MinimumLength(8)
            .WithMessage("Password must be at least 8 characters long.")

            // Security review finding: no upper bound let arbitrarily long input reach the
            // password hasher (PBKDF2 cost scales with input size). 128 is generous for any
            // real passphrase while capping the work a single request can force server-side.
            .MaximumLength(128)
            .WithMessage("Password must be at most 128 characters long.");
    }
}
