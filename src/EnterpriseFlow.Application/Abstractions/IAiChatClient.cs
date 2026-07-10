namespace EnterpriseFlow.Application.Abstractions;

public enum AiChatRole
{
    System,
    User,
    Assistant,
    Tool,
}

/// <summary>
/// F9.1/F9.3 (ADR-0013). One active implementation per instance, selected by
/// <c>Ai:ChatProvider</c> — same pattern as <see cref="IDocumentStorageProvider"/> (ADR-0009).
/// Separate from <see cref="IEmbeddingClient"/> on purpose (ADR-0013's Sprint 3 correction):
/// Anthropic has no embeddings API, so a single interface covering both capabilities would force
/// every chat provider to also be a viable embeddings provider.
/// </summary>
public interface IAiChatClient
{
    /// <summary>
    /// <paramref name="tools"/> is the entire surface of what the model can ask for — each one
    /// is a thin wrapper over an existing Application Query (ADR-0013). The response is either
    /// final text, or a request to invoke one or more tools; the caller resolves those requests
    /// against the real handlers and calls this again with the results appended as
    /// <see cref="AiChatRole.Tool"/> messages, until the model returns final text.
    /// </summary>
    Task<AiChatResponse> SendAsync(
        IReadOnlyList<AiChatMessage> messages,
        IReadOnlyList<AiToolDefinition> tools,
        CancellationToken cancellationToken);
}

/// <summary>
/// <paramref name="ToolCallId"/>/<paramref name="ToolName"/> are only set on a
/// <see cref="AiChatRole.Tool"/> message — the result of a tool the model previously asked to
/// invoke, matched back to that request by id. <paramref name="ToolCalls"/> is only set on an
/// <see cref="AiChatRole.Assistant"/> message that represents a turn where the model asked for
/// one or more tools (<paramref name="Content"/> empty in that case) — both OpenAI and Anthropic
/// require that exact turn to be replayed back verbatim before the matching Tool-role results,
/// found while building <c>OpenAiChatClient</c> (Sprint 7a): a bare Tool message with no
/// preceding Assistant tool-call turn is rejected by OpenAI's API as malformed.
/// </summary>
public sealed record AiChatMessage(
    AiChatRole Role,
    string Content,
    string? ToolCallId = null,
    string? ToolName = null,
    IReadOnlyList<AiToolCallRequest>? ToolCalls = null);

/// <summary><paramref name="ParametersJsonSchema"/> describes the tool's arguments as a JSON
/// Schema string — the shape every provider's function-calling API expects, even though the
/// exact request/response envelope around it differs per provider (resolved inside each
/// concrete <see cref="IAiChatClient"/> implementation, never exposed here).</summary>
public sealed record AiToolDefinition(string Name, string Description, string ParametersJsonSchema);

public sealed record AiToolCallRequest(string Id, string ToolName, string ArgumentsJson);

/// <summary>Exactly one of <paramref name="FinalText"/> or a non-empty <paramref name="ToolCalls"/>
/// is meaningful per response — a model either answers, or asks for tool results before it can.</summary>
public sealed record AiChatResponse(string? FinalText, IReadOnlyList<AiToolCallRequest> ToolCalls);
