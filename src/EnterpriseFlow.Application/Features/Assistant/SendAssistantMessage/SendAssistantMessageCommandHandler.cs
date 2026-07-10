using System.Text.Json;
using EnterpriseFlow.Application.Abstractions;
using EnterpriseFlow.Application.Common;
using EnterpriseFlow.Application.Features.Projects.GetProjects;
using EnterpriseFlow.Application.Features.ProjectTasks.GetMyOverdueTasks;
using EnterpriseFlow.Application.Features.Rag.SearchDocumentChunks;
using EnterpriseFlow.Domain.Entities;
using EnterpriseFlow.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Assistant.SendAssistantMessage;

/// <summary>
/// HU-092 (ADR-0013): the tool-use loop. Every tool invocation goes through <see cref="ISender"/>
/// — the exact same MediatR pipeline (AuthorizationBehavior, tenant query filter) any other
/// caller of that Query goes through. The model never gets a result the calling user couldn't
/// already see through the regular UI.
/// </summary>
public sealed class SendAssistantMessageCommandHandler(
    IAppDbContext db,
    ICurrentUserService currentUser,
    IAiChatClient aiChatClient,
    ISender sender)
    : IRequestHandler<SendAssistantMessageCommand, string>
{
    private const int MaxToolUseIterations = 5;
    private const int HistoryMessageCount = 20;

    private const string SystemPrompt =
        "Sos el asistente de EnterpriseFlow AI. Respondé preguntas del usuario sobre sus propios " +
        "datos (Proyectos, Tareas, Clientes, Documentos) usando exclusivamente las herramientas " +
        "disponibles — nunca inventes datos que no vengan de una herramienta.";

    public async Task<string> Handle(SendAssistantMessageCommand request, CancellationToken cancellationToken)
    {
        var userId = currentUser.UserId;

        db.AssistantMessages.Add(AssistantMessage.Create(userId, AssistantMessageRole.User, request.Message));
        await db.SaveChangesAsync(cancellationToken);

        // Sorted client-side, not via OrderBy in the query — SQLite (integration test suite)
        // can't translate ORDER BY over a DateTimeOffset column server-side, even though SQL
        // Server can; same gap GetAssistantMessagesQueryHandler already guards against
        // (originally found in GetMyNotificationsQueryHandler, Release 2 Sprint 9) — missed here
        // on the first pass, caught by this same Sprint's own integration tests.
        var history = await db.AssistantMessages
            .Where(m => m.UserId == userId)
            .Select(m => new { m.Role, m.Content, m.CreatedAtUtc })
            .ToListAsync(cancellationToken);
        history = history.OrderByDescending(m => m.CreatedAtUtc).Take(HistoryMessageCount).ToList();
        history.Reverse();

        var messages = new List<AiChatMessage> { new(AiChatRole.System, SystemPrompt) };
        messages.AddRange(history.Select(m =>
            new AiChatMessage(m.Role == AssistantMessageRole.User ? AiChatRole.User : AiChatRole.Assistant, m.Content)));

        string? finalText = null;
        for (var iteration = 0; iteration < MaxToolUseIterations && finalText is null; iteration++)
        {
            var response = await aiChatClient.SendAsync(messages, AssistantToolCatalog.All, cancellationToken);

            if (response.FinalText is not null)
            {
                finalText = response.FinalText;
                break;
            }

            // The assistant's own tool-call turn has to be replayed back verbatim before the
            // matching Tool-role results — both OpenAI and Anthropic reject a bare Tool message
            // with no preceding Assistant turn requesting it (found building OpenAiChatClient,
            // Sprint 7a).
            messages.Add(new AiChatMessage(AiChatRole.Assistant, string.Empty, ToolCalls: response.ToolCalls));

            foreach (var toolCall in response.ToolCalls)
            {
                var result = await InvokeToolAsync(toolCall, cancellationToken);
                messages.Add(new AiChatMessage(AiChatRole.Tool, result, toolCall.Id, toolCall.ToolName));
            }
        }

        finalText ??= "No pude completar la respuesta — intentá reformular tu pregunta.";

        db.AssistantMessages.Add(AssistantMessage.Create(userId, AssistantMessageRole.Assistant, finalText));
        await db.SaveChangesAsync(cancellationToken);

        return finalText;
    }

    private static string ExtractQueryArgument(string argumentsJson)
    {
        using var document = JsonDocument.Parse(argumentsJson);
        return document.RootElement.TryGetProperty("query", out var value) ? value.GetString() ?? string.Empty : string.Empty;
    }

    private async Task<string> InvokeToolAsync(AiToolCallRequest toolCall, CancellationToken cancellationToken)
    {
        try
        {
            return toolCall.ToolName switch
            {
                AssistantToolCatalog.GetMyProjects =>
                    JsonSerializer.Serialize(await sender.Send(new GetProjectsQuery(), cancellationToken)),
                AssistantToolCatalog.SearchMyDocuments =>
                    JsonSerializer.Serialize(await sender.Send(
                        new SearchDocumentChunksQuery(ExtractQueryArgument(toolCall.ArgumentsJson)), cancellationToken)),
                AssistantToolCatalog.GetMyOverdueTasks =>
                    JsonSerializer.Serialize(await sender.Send(new GetMyOverdueTasksQuery(), cancellationToken)),
                _ => "Error: herramienta desconocida.",
            };
        }
        catch (ForbiddenAccessException)
        {
            return "Error: no tenés permiso para consultar eso.";
        }
    }
}
