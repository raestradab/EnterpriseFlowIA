using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Documents.GetDocumentById;

public sealed class GetDocumentByIdQueryHandler(IAppDbContext db) : IRequestHandler<GetDocumentByIdQuery, DocumentDto?>
{
    public Task<DocumentDto?> Handle(GetDocumentByIdQuery request, CancellationToken cancellationToken) =>
        (from d in db.Documents
         where d.Id == request.Id
         from w in db.WorkflowDefinitions
         from s in w.States
         where s.Id == d.CurrentWorkflowStateId
         select new DocumentDto(
             d.Id, d.FileName, d.ContentType, d.SizeBytes, d.OwnerType, d.OwnerId, d.CurrentWorkflowStateId, s.Name, w.Id))
        .FirstOrDefaultAsync(cancellationToken);
}
