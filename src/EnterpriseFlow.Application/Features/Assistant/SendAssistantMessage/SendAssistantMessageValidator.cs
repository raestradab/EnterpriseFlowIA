using FluentValidation;

namespace EnterpriseFlow.Application.Features.Assistant.SendAssistantMessage;

public sealed class SendAssistantMessageValidator : AbstractValidator<SendAssistantMessageCommand>
{
    public SendAssistantMessageValidator()
    {
        RuleFor(c => c.Message)
            .NotEmpty()
            .MaximumLength(4000);
    }
}
