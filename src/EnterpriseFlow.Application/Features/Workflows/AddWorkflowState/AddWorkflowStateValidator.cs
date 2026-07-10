using FluentValidation;

namespace EnterpriseFlow.Application.Features.Workflows.AddWorkflowState;

public sealed class AddWorkflowStateValidator : AbstractValidator<AddWorkflowStateCommand>
{
    public AddWorkflowStateValidator()
    {
        RuleFor(c => c.WorkflowId)
            .NotEmpty();

        RuleFor(c => c.Name)
            .NotEmpty()
            .MaximumLength(100);
    }
}
