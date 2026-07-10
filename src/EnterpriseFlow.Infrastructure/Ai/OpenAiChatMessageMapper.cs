using EnterpriseFlow.Application.Abstractions;
using OpenAI.Chat;

namespace EnterpriseFlow.Infrastructure.Ai;

/// <summary>
/// Pure translation to/from OpenAI's wire format — split out of <see cref="OpenAiChatClient"/>
/// so it can be unit-tested without a real HTTP call (no API key in this environment).
/// </summary>
public static class OpenAiChatMessageMapper
{
    public static IEnumerable<ChatMessage> ToOpenAi(IReadOnlyList<AiChatMessage> messages) => messages.Select(ToOpenAi);

    public static ChatMessage ToOpenAi(AiChatMessage message) => message.Role switch
    {
        AiChatRole.System => new SystemChatMessage(message.Content),
        AiChatRole.User => new UserChatMessage(message.Content),
        AiChatRole.Assistant when message.ToolCalls is { Count: > 0 } => new AssistantChatMessage(
            message.ToolCalls.Select(t =>
                ChatToolCall.CreateFunctionToolCall(t.Id, t.ToolName, BinaryData.FromString(t.ArgumentsJson)))),
        AiChatRole.Assistant => new AssistantChatMessage(message.Content),
        AiChatRole.Tool => new ToolChatMessage(
            message.ToolCallId ?? throw new InvalidOperationException("A Tool-role message requires a ToolCallId."), message.Content),
        _ => throw new NotSupportedException($"Unsupported role '{message.Role}'."),
    };

    public static AiChatResponse ToAiChatResponse(ChatCompletion completion)
    {
        if (completion.ToolCalls is { Count: > 0 })
        {
            var toolCalls = completion.ToolCalls
                .Select(t => new AiToolCallRequest(t.Id, t.FunctionName, t.FunctionArguments.ToString()))
                .ToList();
            return new AiChatResponse(null, toolCalls);
        }

        var text = completion.Content.Count > 0 ? completion.Content[0].Text : string.Empty;
        return new AiChatResponse(text, []);
    }
}
