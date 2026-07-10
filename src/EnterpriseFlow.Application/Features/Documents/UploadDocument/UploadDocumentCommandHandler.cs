using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Documents.UploadDocument;

public sealed class UploadDocumentCommandHandler(IAppDbContext db, IDocumentStorageProvider storage)
    : IRequestHandler<UploadDocumentCommand, Guid>
{
    public async Task<Guid> Handle(UploadDocumentCommand request, CancellationToken cancellationToken)
    {
        // ValidationBehavior already confirmed the Workflow exists and has an initial state.
        var workflow = await db.WorkflowDefinitions
            .Include(w => w.States)
            .FirstAsync(w => w.Id == request.WorkflowDefinitionId, cancellationToken);
        var initialStateId = workflow.States.First(s => s.IsInitial).Id;

        request.Content.Position = 0;
        var storageKey = await storage.UploadAsync(request.Content, request.FileName, request.ContentType, cancellationToken);

        var document = Document.Create(
            request.FileName,
            request.ContentType,
            request.SizeBytes,
            request.OwnerType,
            request.OwnerId,
            storageKey,
            initialStateId);

        db.Documents.Add(document);
        await db.SaveChangesAsync(cancellationToken);

        return document.Id;
    }
}
