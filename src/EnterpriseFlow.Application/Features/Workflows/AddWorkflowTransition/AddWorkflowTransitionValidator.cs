using FluentValidation;

namespace EnterpriseFlow.Application.Features.Workflows.AddWorkflowTransition;

public sealed class AddWorkflowTransitionValidator : AbstractValidator<AddWorkflowTransitionCommand>
{
    public AddWorkflowTransitionValidator()
    {
        RuleFor(c => c.WorkflowId)
            .NotEmpty();

        RuleFor(c => c.Name)
            .NotEmpty()
            .MaximumLength(100);

        RuleFor(c => c.FromStateId)
            .NotEmpty();

        RuleFor(c => c.ToStateId)
            .NotEmpty();
    }
}
