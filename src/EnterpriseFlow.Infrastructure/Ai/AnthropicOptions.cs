namespace EnterpriseFlow.Infrastructure.Ai;

/// <summary>Bound from the <c>Ai:Anthropic</c> configuration section — read whenever
/// <c>Ai:ChatProvider</c> is <c>"anthropic"</c> (see DependencyInjection.cs). No embeddings
/// model here — Anthropic has no embeddings API (ADR-0013's Sprint 3 correction), so
/// <c>Ai:EmbeddingProvider</c> can never resolve to this provider.</summary>
public sealed class AnthropicOptions
{
    public const string SectionName = "Ai:Anthropic";

    public required string ApiKey { get; init; }

    public string Model { get; init; } = "claude-3-5-sonnet-20241022";
}
