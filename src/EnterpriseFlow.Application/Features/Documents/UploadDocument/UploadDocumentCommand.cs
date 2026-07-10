using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Enums;
using MediatR;

namespace EnterpriseFlow.Application.Features.Documents.UploadDocument;

/// <summary>HU-050/HU-080. <see cref="WorkflowDefinitionId"/> is explicit rather than implied —
/// F8.1's whole premise is that a tenant can have more than one Workflow, so the caller (not
/// Domain) decides which one governs this Document.</summary>
public sealed record UploadDocumentCommand(
    string FileName,
    string ContentType,
    long SizeBytes,
    Stream Content,
    DocumentOwnerType OwnerType,
    Guid OwnerId,
    Guid WorkflowDefinitionId) : IRequest<Guid>, IRequirePermission
{
    public string RequiredPermission => Permissions.Documents.Manage;
}
