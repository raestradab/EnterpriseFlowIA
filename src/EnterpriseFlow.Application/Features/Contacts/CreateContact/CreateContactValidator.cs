using EnterpriseFlow.Application.Abstractions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Contacts.CreateContact;

public sealed class CreateContactValidator : AbstractValidator<CreateContactCommand>
{
    public CreateContactValidator(IAppDbContext db)
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(c => c.Email)
            .EmailAddress()
            .When(c => !string.IsNullOrWhiteSpace(c.Email));

        // Same reasoning as CreateClientValidator: the lookup is tenant-filtered (ADR-0003),
        // so a Client from another tenant simply won't be found.
        RuleFor(c => c.ClientId)
            .MustAsync((clientId, ct) => db.Clients.AnyAsync(c => c.Id == clientId, ct))
            .WithMessage("The specified Client does not exist.");
    }
}
