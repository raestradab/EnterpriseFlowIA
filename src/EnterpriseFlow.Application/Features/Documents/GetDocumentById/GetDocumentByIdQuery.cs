using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Enums;
using MediatR;

namespace EnterpriseFlow.Application.Features.Documents.GetDocumentById;

public sealed record GetDocumentByIdQuery(Guid Id) : IRequest<DocumentDto?>, IRequirePermission
{
    public string RequiredPermission => Permissions.Documents.Read;
}

/// <summary>HU-081: <see cref="CurrentWorkflowStateName"/> and <see cref="WorkflowDefinitionId"/>
/// exist because the first real consumer (Sprint 8b frontend) needs them — a UI can't show a
/// readable state or resolve which transitions are valid from a bare <c>CurrentWorkflowStateId</c>
/// alone. <c>Document</c> itself only stores the state id (ADR-0010); both are resolved here by
/// joining to the owning <c>WorkflowDefinition</c>.</summary>
public sealed record DocumentDto(
    Guid Id,
    string FileName,
    string ContentType,
    long SizeBytes,
    DocumentOwnerType OwnerType,
    Guid OwnerId,
    Guid CurrentWorkflowStateId,
    string CurrentWorkflowStateName,
    Guid WorkflowDefinitionId);
