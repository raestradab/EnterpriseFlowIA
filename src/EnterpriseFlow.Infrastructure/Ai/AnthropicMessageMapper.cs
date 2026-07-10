using System.Text.Json.Nodes;
using EnterpriseFlow.Application.Abstractions;

namespace EnterpriseFlow.Infrastructure.Ai;

/// <summary>
/// Pure translation to/from Anthropic's Messages API wire format — split out of
/// <see cref="AnthropicChatClient"/> so it can be unit-tested without a real HTTP call (no API
/// key in this environment). Anthropic's protocol shape is meaningfully different from OpenAI's:
/// <c>system</c> is a top-level request field, not a message; the <c>messages</c> array only has
/// <c>user</c>/<c>assistant</c> roles — tool requests are <c>tool_use</c> content blocks inside an
/// <c>assistant</c> message, and tool results are <c>tool_result</c> content blocks inside a
/// <c>user</c> message (multiple results answering the same assistant turn must be merged into
/// one <c>user</c> turn, not sent as separate consecutive messages).
/// </summary>
public static class AnthropicMessageMapper
{
    public static JsonObject BuildRequestBody(string model, IReadOnlyList<AiChatMessage> messages, IReadOnlyList<AiToolDefinition> tools)
    {
        var (system, anthropicMessages) = ToAnthropicMessages(messages);

        var body = new JsonObject
        {
            ["model"] = model,
            ["max_tokens"] = 1024,
            ["messages"] = anthropicMessages,
        };

        if (system is not null)
        {
            body["system"] = system;
        }

        if (tools.Count > 0)
        {
            var toolsArray = new JsonArray();
            foreach (var tool in tools)
            {
                toolsArray.Add(new JsonObject
                {
                    ["name"] = tool.Name,
                    ["description"] = tool.Description,
                    ["input_schema"] = JsonNode.Parse(tool.ParametersJsonSchema),
                });
            }

            body["tools"] = toolsArray;
        }

        return body;
    }

    public static (string? System, JsonArray Messages) ToAnthropicMessages(IReadOnlyList<AiChatMessage> messages)
    {
        var systemParts = new List<string>();
        var result = new JsonArray();

        foreach (var message in messages)
        {
            switch (message.Role)
            {
                case AiChatRole.System:
                    systemParts.Add(message.Content);
                    break;

                case AiChatRole.User:
                    result.Add(new JsonObject { ["role"] = "user", ["content"] = message.Content });
                    break;

                case AiChatRole.Assistant when message.ToolCalls is { Count: > 0 }:
                    var toolUseBlocks = new JsonArray();
                    foreach (var toolCall in message.ToolCalls)
                    {
                        toolUseBlocks.Add(new JsonObject
                        {
                            ["type"] = "tool_use",
                            ["id"] = toolCall.Id,
                            ["name"] = toolCall.ToolName,
                            ["input"] = JsonNode.Parse(toolCall.ArgumentsJson),
                        });
                    }

                    result.Add(new JsonObject { ["role"] = "assistant", ["content"] = toolUseBlocks });
                    break;

                case AiChatRole.Assistant:
                    result.Add(new JsonObject { ["role"] = "assistant", ["content"] = message.Content });
                    break;

                case AiChatRole.Tool:
                    AppendToolResult(result, message);
                    break;
            }
        }

        var system = systemParts.Count > 0 ? string.Join("\n", systemParts) : null;
        return (system, result);
    }

    public static AiChatResponse ParseResponse(JsonObject response)
    {
        var contentArray = response["content"]?.AsArray() ?? [];
        var toolCalls = new List<AiToolCallRequest>();
        string? text = null;

        foreach (var block in contentArray)
        {
            if (block is not JsonObject blockObj)
            {
                continue;
            }

            switch (blockObj["type"]?.GetValue<string>())
            {
                case "text":
                    text = (text ?? string.Empty) + blockObj["text"]?.GetValue<string>();
                    break;
                case "tool_use":
                    toolCalls.Add(new AiToolCallRequest(
                        blockObj["id"]!.GetValue<string>(),
                        blockObj["name"]!.GetValue<string>(),
                        blockObj["input"]?.ToJsonString() ?? "{}"));
                    break;
            }
        }

        return toolCalls.Count > 0 ? new AiChatResponse(null, toolCalls) : new AiChatResponse(text ?? string.Empty, []);
    }

    private static void AppendToolResult(JsonArray messages, AiChatMessage message)
    {
        var toolResultBlock = new JsonObject
        {
            ["type"] = "tool_result",
            ["tool_use_id"] = message.ToolCallId ?? throw new InvalidOperationException("A Tool-role message requires a ToolCallId."),
            ["content"] = message.Content,
        };

        if (messages.Count > 0
            && messages[^1] is JsonObject lastMessage
            && lastMessage["role"]?.GetValue<string>() == "user"
            && lastMessage["content"] is JsonArray { Count: > 0 } lastContent
            && lastContent[0] is JsonObject { } firstBlock
            && firstBlock["type"]?.GetValue<string>() == "tool_result")
        {
            lastContent.Add(toolResultBlock);
        }
        else
        {
            messages.Add(new JsonObject { ["role"] = "user", ["content"] = new JsonArray(toolResultBlock) });
        }
    }
}
