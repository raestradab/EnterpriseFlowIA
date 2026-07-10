using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Domain.Enums;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EnterpriseFlow.Application.Features.Documents.UploadDocument;

public sealed class UploadDocumentValidator : AbstractValidator<UploadDocumentCommand>
{
    public UploadDocumentValidator(IAppDbContext db, IOptions<DocumentValidationOptions> options)
    {
        var settings = options.Value;

        RuleFor(c => c.FileName)
            .NotEmpty()
            .Must(name => settings.AllowedExtensions.Contains(Path.GetExtension(name), StringComparer.OrdinalIgnoreCase))
            .WithMessage("File extension is not allowed.");

        RuleFor(c => c.SizeBytes)
            .GreaterThan(0)
            .LessThanOrEqualTo(settings.MaxSizeBytes)
            .WithMessage($"File exceeds the maximum allowed size of {settings.MaxSizeBytes} bytes.");

        RuleFor(c => c.Content)
            .Must((command, content) => MatchesDeclaredExtension(content, command.FileName))
            .WithMessage("File content does not match its declared extension.");

        RuleFor(c => c.OwnerId)
            .MustAsync((command, ownerId, ct) => OwnerExistsAsync(db, command.OwnerType, ownerId, ct))
            .WithMessage("The specified owner does not exist.");

        RuleFor(c => c.WorkflowDefinitionId)
            .MustAsync((workflowId, ct) => HasInitialStateAsync(db, workflowId, ct))
            .WithMessage("The specified Workflow does not exist or has no initial state defined.");
    }

    private static bool MatchesDeclaredExtension(Stream content, string fileName)
    {
        Span<byte> header = stackalloc byte[8];
        content.Position = 0;
        var bytesRead = content.Read(header);
        content.Position = 0;

        return FileSignatureValidator.MatchesExtension(header[..bytesRead], Path.GetExtension(fileName));
    }

    private static Task<bool> OwnerExistsAsync(IAppDbContext db, DocumentOwnerType ownerType, Guid ownerId, CancellationToken ct) =>
        ownerType switch
        {
            DocumentOwnerType.Project => db.Projects.AnyAsync(p => p.Id == ownerId, ct),
            DocumentOwnerType.Client => db.Clients.AnyAsync(c => c.Id == ownerId, ct),
            DocumentOwnerType.Task => db.ProjectTasks.AnyAsync(t => t.Id == ownerId, ct),
            _ => Task.FromResult(false),
        };

    private static async Task<bool> HasInitialStateAsync(IAppDbContext db, Guid workflowId, CancellationToken ct)
    {
        var workflow = await db.WorkflowDefinitions
            .Include(w => w.States)
            .FirstOrDefaultAsync(w => w.Id == workflowId, ct);

        return workflow is not null && workflow.States.Any(s => s.IsInitial);
    }
}
