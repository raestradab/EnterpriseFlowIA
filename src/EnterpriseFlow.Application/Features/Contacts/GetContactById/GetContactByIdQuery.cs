using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Contacts.GetContactById;

public sealed record GetContactByIdQuery(Guid Id) : IRequest<ContactDto?>, IRequirePermission
{
    public string RequiredPermission => Permissions.Contacts.Read;
}

public sealed record ContactDto(Guid Id, string Name, string? Email, string? Phone, Guid ClientId);
