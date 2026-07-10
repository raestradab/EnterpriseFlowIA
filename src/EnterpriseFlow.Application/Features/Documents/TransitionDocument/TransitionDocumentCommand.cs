using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Documents.TransitionDocument;

public sealed record TransitionDocumentCommand(Guid DocumentId, Guid TargetStateId) : IRequest, IRequirePermission
{
    public string RequiredPermission => Permissions.Documents.Approve;
}
