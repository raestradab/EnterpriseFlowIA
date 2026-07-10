using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Contacts.GetContactById;

public sealed class GetContactByIdQueryHandler(IAppDbContext db) : IRequestHandler<GetContactByIdQuery, ContactDto?>
{
    public Task<ContactDto?> Handle(GetContactByIdQuery request, CancellationToken cancellationToken) =>
        db.Contacts
            .Where(c => c.Id == request.Id)
            .Select(c => new ContactDto(c.Id, c.Name, c.Email, c.Phone, c.ClientId))
            .FirstOrDefaultAsync(cancellationToken);
}
