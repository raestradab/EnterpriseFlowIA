using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Enums;
using MediatR;

namespace EnterpriseFlow.Application.Features.Documents.GetDocuments;

public sealed record GetDocumentsQuery(DocumentOwnerType OwnerType, Guid OwnerId)
    : IRequest<IReadOnlyCollection<DocumentListItemDto>>, IRequirePermission
{
    public string RequiredPermission => Permissions.Documents.Read;
}

/// <summary>See <c>GetDocumentById.DocumentDto</c> for why <see cref="CurrentWorkflowStateName"/>
/// and <see cref="WorkflowDefinitionId"/> are resolved here too.</summary>
public sealed record DocumentListItemDto(
    Guid Id,
    string FileName,
    long SizeBytes,
    Guid CurrentWorkflowStateId,
    string CurrentWorkflowStateName,
    Guid WorkflowDefinitionId);
