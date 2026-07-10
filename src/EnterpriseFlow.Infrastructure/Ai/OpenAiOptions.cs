namespace EnterpriseFlow.Infrastructure.Ai;

/// <summary>Bound from the <c>Ai:OpenAi</c> configuration section — read whenever
/// <c>Ai:ChatProvider</c> or <c>Ai:EmbeddingProvider</c> is <c>"openai"</c>
/// (see DependencyInjection.cs).</summary>
public sealed class OpenAiOptions
{
    public const string SectionName = "Ai:OpenAi";

    public required string ApiKey { get; init; }

    public string ChatModel { get; init; } = "gpt-4o-mini";

    public string EmbeddingModel { get; init; } = "text-embedding-3-small";
}
