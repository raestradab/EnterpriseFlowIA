using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Contacts.CreateContact;

public sealed record CreateContactCommand(string Name, string? Email, string? Phone, Guid ClientId)
    : IRequest<Guid>, IRequirePermission
{
    public string RequiredPermission => Permissions.Contacts.Manage;
}
