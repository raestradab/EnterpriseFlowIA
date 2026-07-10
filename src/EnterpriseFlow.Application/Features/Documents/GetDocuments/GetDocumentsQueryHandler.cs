using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Documents.GetDocuments;

public sealed class GetDocumentsQueryHandler(IAppDbContext db)
    : IRequestHandler<GetDocumentsQuery, IReadOnlyCollection<DocumentListItemDto>>
{
    public async Task<IReadOnlyCollection<DocumentListItemDto>> Handle(GetDocumentsQuery request, CancellationToken cancellationToken) =>
        await (from d in db.Documents
               where d.OwnerType == request.OwnerType && d.OwnerId == request.OwnerId
               from w in db.WorkflowDefinitions
               from s in w.States
               where s.Id == d.CurrentWorkflowStateId
               select new DocumentListItemDto(d.Id, d.FileName, d.SizeBytes, d.CurrentWorkflowStateId, s.Name, w.Id))
        .ToListAsync(cancellationToken);
}
