using EnterpriseFlow.Api.Authorization;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Documents.DeleteDocument;
using EnterpriseFlow.Application.Features.Documents.DownloadDocument;
using EnterpriseFlow.Application.Features.Documents.GetDocumentById;
using EnterpriseFlow.Application.Features.Documents.GetDocuments;
using EnterpriseFlow.Application.Features.Documents.TransitionDocument;
using EnterpriseFlow.Application.Features.Documents.UploadDocument;
using EnterpriseFlow.Domain.Enums;
using MediatR;

namespace EnterpriseFlow.Api.Endpoints;

public static class DocumentsEndpoints
{
    public static IEndpointRouteBuilder MapDocumentsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/documents").WithTags("Documents");

        group.MapPost("/", UploadAsync)
            .RequirePermission(Permissions.Documents.Manage)
            .WithSummary("Uploads a Document.")
            .WithDescription("multipart/form-data, not JSON — hand-parsed (see UploadAsync below), so this " +
                "contract isn't visible from a request DTO schema. Required form fields: \"file\" (the binary " +
                "content), \"ownerType\" (Project/Client/Task), \"ownerId\", and \"workflowDefinitionId\" (the " +
                "Workflow the Document enters at its initial state, HU-081).");

        group.MapGet("/", async (DocumentOwnerType ownerType, Guid ownerId, ISender sender, CancellationToken ct) =>
                Results.Ok(await sender.Send(new GetDocumentsQuery(ownerType, ownerId), ct)))
            .RequirePermission(Permissions.Documents.Read);

        group.MapGet("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var document = await sender.Send(new GetDocumentByIdQuery(id), ct);
                return document is null ? Results.NotFound() : Results.Ok(document);
            })
            .RequirePermission(Permissions.Documents.Read);

        group.MapGet("/{id:guid}/content", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                var result = await sender.Send(new DownloadDocumentQuery(id), ct);
                return result is null ? Results.NotFound() : Results.Stream(result.Content, result.ContentType, result.FileName);
            })
            .RequirePermission(Permissions.Documents.Read);

        group.MapPost("/{id:guid}/transition", async (Guid id, TransitionDocumentRequest request, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new TransitionDocumentCommand(id, request.TargetStateId), ct);
                return Results.NoContent();
            })
            .RequirePermission(Permissions.Documents.Approve);

        group.MapDelete("/{id:guid}", async (Guid id, ISender sender, CancellationToken ct) =>
            {
                await sender.Send(new DeleteDocumentCommand(id), ct);
                return Results.NoContent();
            })
            .RequirePermission(Permissions.Documents.Manage);

        return app;
    }

    private static async Task<IResult> UploadAsync(HttpRequest request, ISender sender, CancellationToken ct)
    {
        if (!request.HasFormContentType)
        {
            return Results.BadRequest("Expected multipart/form-data.");
        }

        var form = await request.ReadFormAsync(ct);
        var file = form.Files.GetFile("file");
        if (file is null || file.Length == 0)
        {
            return Results.BadRequest("No file was uploaded.");
        }

        if (!Enum.TryParse<DocumentOwnerType>(form["ownerType"], ignoreCase: true, out var ownerType)
            || !Guid.TryParse(form["ownerId"], out var ownerId)
            || !Guid.TryParse(form["workflowDefinitionId"], out var workflowDefinitionId))
        {
            return Results.BadRequest("ownerType, ownerId and workflowDefinitionId are required form fields.");
        }

        await using var stream = file.OpenReadStream();
        var command = new UploadDocumentCommand(
            file.FileName,
            file.ContentType,
            file.Length,
            stream,
            ownerType,
            ownerId,
            workflowDefinitionId);

        var id = await sender.Send(command, ct);
        return Results.Created($"/api/documents/{id}", new { id });
    }

    private sealed record TransitionDocumentRequest(Guid TargetStateId);
}
