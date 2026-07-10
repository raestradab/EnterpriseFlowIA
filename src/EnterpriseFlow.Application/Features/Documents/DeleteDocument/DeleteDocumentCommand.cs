using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Documents.DeleteDocument;

public sealed record DeleteDocumentCommand(Guid Id) : IRequest, IRequirePermission
{
    public string RequiredPermission => Permissions.Documents.Manage;
}
