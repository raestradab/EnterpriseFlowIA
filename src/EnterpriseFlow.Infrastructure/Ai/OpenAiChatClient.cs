using EnterpriseFlow.Application.Abstractions;
using Microsoft.Extensions.Options;
using OpenAI.Chat;

namespace EnterpriseFlow.Infrastructure.Ai;

/// <summary>
/// F9.1 (ADR-0013), real implementation via the official <c>OpenAI</c> SDK. Registered when
/// <c>Ai:ChatProvider=openai</c>. <b>Not runtime-verified against the real API</b> — no API key
/// is available in this environment (r3-01-vision-y-alcance.md, sección 0); the mapping to/from
/// OpenAI's wire format is covered by <c>OpenAiChatMessageMapperTests</c> instead, which exercise
/// the pure translation logic without a network call.
/// </summary>
public sealed class OpenAiChatClient : IAiChatClient
{
    private readonly ChatClient _chatClient;

    public OpenAiChatClient(IOptions<OpenAiOptions> options)
    {
        var settings = options.Value;
        _chatClient = new ChatClient(settings.ChatModel, settings.ApiKey);
    }

    public async Task<AiChatResponse> SendAsync(
        IReadOnlyList<AiChatMessage> messages, IReadOnlyList<AiToolDefinition> tools, CancellationToken cancellationToken)
    {
        var options = new ChatCompletionOptions();
        foreach (var tool in tools)
        {
            options.Tools.Add(ChatTool.CreateFunctionTool(tool.Name, tool.Description, BinaryData.FromString(tool.ParametersJsonSchema)));
        }

        var completion = await _chatClient.CompleteChatAsync(OpenAiChatMessageMapper.ToOpenAi(messages), options, cancellationToken);

        return OpenAiChatMessageMapper.ToAiChatResponse(completion.Value);
    }
}
