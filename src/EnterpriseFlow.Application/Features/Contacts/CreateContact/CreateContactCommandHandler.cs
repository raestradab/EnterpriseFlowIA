using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Domain.Entities;
using MediatR;

namespace EnterpriseFlow.Application.Features.Contacts.CreateContact;

public sealed class CreateContactCommandHandler(IAppDbContext db) : IRequestHandler<CreateContactCommand, Guid>
{
    public async Task<Guid> Handle(CreateContactCommand request, CancellationToken cancellationToken)
    {
        var contact = Contact.Create(request.Name, request.Email, request.Phone, request.ClientId);

        db.Contacts.Add(contact);
        await db.SaveChangesAsync(cancellationToken);

        return contact.Id;
    }
}
