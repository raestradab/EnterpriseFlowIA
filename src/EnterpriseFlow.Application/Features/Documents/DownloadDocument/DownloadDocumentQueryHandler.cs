using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Documents.DownloadDocument;

public sealed class DownloadDocumentQueryHandler(IAppDbContext db, IDocumentStorageProvider storage)
    : IRequestHandler<DownloadDocumentQuery, DownloadDocumentResult?>
{
    public async Task<DownloadDocumentResult?> Handle(DownloadDocumentQuery request, CancellationToken cancellationToken)
    {
        var document = await db.Documents
            .Where(d => d.Id == request.Id)
            .Select(d => new { d.FileName, d.ContentType, d.StorageKey })
            .FirstOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            return null;
        }

        var content = await storage.DownloadAsync(document.StorageKey, cancellationToken);
        return new DownloadDocumentResult(content, document.FileName, document.ContentType);
    }
}
