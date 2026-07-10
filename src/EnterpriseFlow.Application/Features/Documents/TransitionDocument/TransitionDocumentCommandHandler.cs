using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Entities;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Documents.TransitionDocument;

/// <summary>
/// HU-081. <see cref="WorkflowDefinition.CanTransition"/> is the fact resolved here and handed
/// to <see cref="Document.TransitionTo"/> — same "hecho inyectado" split as
/// <c>CloseProjectCommandHandler</c>/<c>Project.Close</c> (ADR-0005), applied to ADR-0010's
/// workflow engine instead of a fixed enum.
/// </summary>
public sealed class TransitionDocumentCommandHandler(IAppDbContext db) : IRequestHandler<TransitionDocumentCommand>
{
    public async Task Handle(TransitionDocumentCommand request, CancellationToken cancellationToken)
    {
        var document = await db.Documents.FirstOrDefaultAsync(d => d.Id == request.DocumentId, cancellationToken)
            ?? throw new NotFoundException(nameof(Document), request.DocumentId);

        var workflow = await db.WorkflowDefinitions
            .Include(w => w.States)
            .Include(w => w.Transitions)
            .FirstOrDefaultAsync(w => w.States.Any(s => s.Id == document.CurrentWorkflowStateId), cancellationToken)
            ?? throw new NotFoundException(nameof(WorkflowDefinition), document.CurrentWorkflowStateId);

        var isAllowed = workflow.CanTransition(document.CurrentWorkflowStateId, request.TargetStateId);
        var targetStateName = workflow.States.FirstOrDefault(s => s.Id == request.TargetStateId)?.Name ?? string.Empty;

        document.TransitionTo(request.TargetStateId, targetStateName, isAllowed);

        await db.SaveChangesAsync(cancellationToken);
    }
}
