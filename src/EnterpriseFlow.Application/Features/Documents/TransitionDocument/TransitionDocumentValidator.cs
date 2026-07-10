using FluentValidation;

namespace EnterpriseFlow.Application.Features.Documents.TransitionDocument;

public sealed class TransitionDocumentValidator : AbstractValidator<TransitionDocumentCommand>
{
    public TransitionDocumentValidator()
    {
        RuleFor(c => c.DocumentId)
            .NotEmpty();

        RuleFor(c => c.TargetStateId)
            .NotEmpty();
    }
}
