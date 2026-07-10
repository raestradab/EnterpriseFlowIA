using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Rag.SearchDocumentChunks;

/// <summary>HU-101 (ADR-0013/ADR-0014). Gated by the same permission as reading Documents
/// directly — grounding an assistant answer in a Document's content is not a lesser privilege
/// than reading that Document through the regular UI.</summary>
public sealed record SearchDocumentChunksQuery(string QueryText) : IRequest<IReadOnlyCollection<DocumentChunkSearchResultDto>>, IRequirePermission
{
    public string RequiredPermission => Permissions.Documents.Read;
}

public sealed record DocumentChunkSearchResultDto(Guid DocumentId, string FileName, string Content, double Score);
