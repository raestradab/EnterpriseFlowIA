using System.Text;
using System.Text.Json.Nodes;
using EnterpriseFlow.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace EnterpriseFlow.Infrastructure.Ai;

/// <summary>
/// F9.1 (ADR-0013). No official Anthropic .NET SDK exists — implemented directly against the
/// public Messages API (<c>POST /v1/messages</c>) rather than pulling in an unofficial
/// third-party package for something this central. Registered when <c>Ai:ChatProvider=anthropic</c>
/// (never for <c>Ai:EmbeddingProvider</c> — Anthropic has no embeddings API).
/// <b>Not runtime-verified against the real API</b> — no API key is available in this environment
/// (r3-01-vision-y-alcance.md, sección 0); the request/response mapping is covered by
/// <c>AnthropicMessageMapperTests</c> instead, which exercise the pure translation logic without
/// a network call.
/// </summary>
public sealed class AnthropicChatClient : IAiChatClient
{
    private const string AnthropicVersion = "2023-06-01";

    private readonly HttpClient _httpClient;
    private readonly string _model;

    public AnthropicChatClient(HttpClient httpClient, IOptions<AnthropicOptions> options)
    {
        _httpClient = httpClient;
        _httpClient.BaseAddress = new Uri("https://api.anthropic.com/");
        _httpClient.DefaultRequestHeaders.Add("x-api-key", options.Value.ApiKey);
        _httpClient.DefaultRequestHeaders.Add("anthropic-version", AnthropicVersion);
        _model = options.Value.Model;
    }

    public async Task<AiChatResponse> SendAsync(
        IReadOnlyList<AiChatMessage> messages, IReadOnlyList<AiToolDefinition> tools, CancellationToken cancellationToken)
    {
        var requestBody = AnthropicMessageMapper.BuildRequestBody(_model, messages, tools);
        using var content = new StringContent(requestBody.ToJsonString(), Encoding.UTF8, "application/json");

        using var response = await _httpClient.PostAsync("v1/messages", content, cancellationToken);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseBody = JsonNode.Parse(responseJson)!.AsObject();

        return AnthropicMessageMapper.ParseResponse(responseBody);
    }
}
