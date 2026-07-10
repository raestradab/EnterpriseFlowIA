using FluentValidation;

namespace EnterpriseFlow.Application.Features.Workflows.CreateWorkflow;

public sealed class CreateWorkflowValidator : AbstractValidator<CreateWorkflowCommand>
{
    public CreateWorkflowValidator()
    {
        RuleFor(c => c.Name)
            .NotEmpty()
            .MaximumLength(200);
    }
}
