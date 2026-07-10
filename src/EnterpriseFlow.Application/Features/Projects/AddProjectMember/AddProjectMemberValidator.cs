using EnterpriseFlow.Application.Abstractions;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Projects.AddProjectMember;

public sealed class AddProjectMemberValidator : AbstractValidator<AddProjectMemberCommand>
{
    public AddProjectMemberValidator(IAppDbContext db)
    {
        RuleFor(c => c.UserId)
            .MustAsync((userId, ct) => db.Users.AnyAsync(u => u.Id == userId, ct))
            .WithMessage("The specified User does not exist.");
    }
}
