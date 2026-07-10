using EnterpriseFlow.Application.Abstractions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Projects.CreateProject;

public sealed class CreateProjectValidator : AbstractValidator<CreateProjectCommand>
{
    public CreateProjectValidator(IAppDbContext db)
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(c => c.ClientId)
            .MustAsync((clientId, ct) => db.Clients.AnyAsync(c => c.Id == clientId, ct))
            .WithMessage("The specified Client does not exist.");

        RuleFor(c => c)
            .Must(c => c.StartDate is null || c.EstimatedEndDate is null || c.EstimatedEndDate >= c.StartDate)
            .WithMessage("Estimated end date cannot be before the start date.")
            .WithName(nameof(CreateProjectCommand.EstimatedEndDate));
    }
}
