using EnterpriseFlow.Application.Abstractions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.ProjectTasks.CreateTask;

public sealed class CreateTaskValidator : AbstractValidator<CreateTaskCommand>
{
    public CreateTaskValidator(IAppDbContext db)
    {
        RuleFor(c => c.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(c => c.Description)
            .MaximumLength(2000);

        RuleFor(c => c.ProjectId)
            .MustAsync((projectId, ct) => db.Projects.AnyAsync(p => p.Id == projectId, ct))
            .WithMessage("The specified Project does not exist.");
    }
}
