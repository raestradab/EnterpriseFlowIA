using EnterpriseFlow.Application.Common;
using MediatR;

namespace EnterpriseFlow.Application.Features.Documents.DownloadDocument;

public sealed record DownloadDocumentQuery(Guid Id) : IRequest<DownloadDocumentResult?>, IRequirePermission
{
    public string RequiredPermission => Permissions.Documents.Read;
}

public sealed record DownloadDocumentResult(Stream Content, string FileName, string ContentType);
