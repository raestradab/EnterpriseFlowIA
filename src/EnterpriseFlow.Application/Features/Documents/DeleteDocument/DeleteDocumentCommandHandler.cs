using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Documents.DeleteDocument;

/// <summary>The physical file is removed from storage as part of deletion — an orphaned blob
/// has no audit value the soft-deleted DB row (which keeps FileName/OwnerId/timestamps) doesn't
/// already provide, and paying to store it forever isn't a real requirement any HU asked for.</summary>
public sealed class DeleteDocumentCommandHandler(IAppDbContext db, IDocumentStorageProvider storage)
    : IRequestHandler<DeleteDocumentCommand>
{
    public async Task Handle(DeleteDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await db.Documents.FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken)
            ?? throw new NotFoundException(nameof(Document), request.Id);

        await storage.DeleteAsync(document.StorageKey, cancellationToken);

        document.MarkDeleted();

        await db.SaveChangesAsync(cancellationToken);
    }
}
