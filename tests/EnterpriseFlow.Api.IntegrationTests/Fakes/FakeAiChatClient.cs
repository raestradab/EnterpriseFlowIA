using System.Text.Json;
using EnterpriseFlow.Application.Abstractions;

namespace EnterpriseFlow.Api.IntegrationTests.Fakes;

/// <summary>
/// Sprint 4 (Validación), Release 3: a real two-round-trip tool-use loop, not a canned answer —
/// no API keys are available in this environment (r3-01-vision-y-alcance.md, sección 0), so this
/// is what proves <c>SendAssistantMessageCommandHandler</c>'s orchestration actually works: on
/// the first call (no Tool message yet), it picks exactly one tool based on the user's own
/// question (Sprint 7b/7c: "proyecto(s)" → <c>get_my_projects</c>, "tarea(s)"/"atrasad"/"vencid"
/// → <c>get_my_overdue_tasks</c>, anything else → <c>search_my_documents</c>, with that same
/// question as the tool's "query" argument) — never every available tool at once, so tests stay
/// deterministic about which real Query ran. Once it sees a Tool-role message in the
/// conversation, it answers using that message's real content — which only exists if a genuine
/// Application Query already ran and returned genuine, tenant-scoped data (ADR-0013).
/// </summary>
public sealed class FakeAiChatClient : IAiChatClient
{
    public Task<AiChatResponse> SendAsync(
        IReadOnlyList<AiChatMessage> messages, IReadOnlyList<AiToolDefinition> tools, CancellationToken cancellationToken)
    {
        var lastMessage = messages.Count > 0 ? messages[^1] : null;

        if (lastMessage is { Role: AiChatRole.Tool })
        {
            return Task.FromResult(new AiChatResponse($"Según la herramienta '{lastMessage.ToolName}': {lastMessage.Content}", []));
        }

        if (tools.Count > 0)
        {
            var userQuestion = messages.LastOrDefault(m => m.Role == AiChatRole.User)?.Content ?? string.Empty;
            var tool = SelectTool(userQuestion, tools);

            var argumentsJson = tool.ParametersJsonSchema.Contains("\"query\"", StringComparison.Ordinal)
                ? JsonSerializer.Serialize(new { query = userQuestion })
                : "{}";

            return Task.FromResult(new AiChatResponse(null, [new AiToolCallRequest(Guid.NewGuid().ToString(), tool.Name, argumentsJson)]));
        }

        return Task.FromResult(new AiChatResponse("No tengo herramientas disponibles para responder eso.", []));
    }

    private static AiToolDefinition SelectTool(string userQuestion, IReadOnlyList<AiToolDefinition> tools)
    {
        if (userQuestion.Contains("proyecto", StringComparison.OrdinalIgnoreCase))
        {
            return tools.FirstOrDefault(t => t.Name == "get_my_projects") ?? tools[0];
        }

        if (userQuestion.Contains("tarea", StringComparison.OrdinalIgnoreCase)
            || userQuestion.Contains("atrasad", StringComparison.OrdinalIgnoreCase)
            || userQuestion.Contains("vencid", StringComparison.OrdinalIgnoreCase))
        {
            return tools.FirstOrDefault(t => t.Name == "get_my_overdue_tasks") ?? tools[0];
        }

        return tools.FirstOrDefault(t => t.Name == "search_my_documents") ?? tools[0];
    }
}
