using EnterpriseFlow.Application.Abstractions;

namespace EnterpriseFlow.Infrastructure.Ai;

/// <summary>
/// Sprint 3 (Arquitectura), Release 3: registered when no real provider is configured
/// (<c>Ai:ChatProvider</c> unset) — same graceful-degradation shape as <c>NullEmailQueue</c>
/// (Release 2). Unlike a true no-op, it returns a message explaining why, rather than silence or
/// an unhandled exception: no API keys are available in this environment (see
/// r3-01-vision-y-alcance.md, sección 0), and a user asking the assistant something deserves to
/// know it isn't configured, not a blank response.
/// </summary>
public sealed class NullAiChatClient : IAiChatClient
{
    public Task<AiChatResponse> SendAsync(
        IReadOnlyList<AiChatMessage> messages, IReadOnlyList<AiToolDefinition> tools, CancellationToken cancellationToken) =>
        Task.FromResult(new AiChatResponse("El asistente de IA no está configurado en este entorno.", []));
}
