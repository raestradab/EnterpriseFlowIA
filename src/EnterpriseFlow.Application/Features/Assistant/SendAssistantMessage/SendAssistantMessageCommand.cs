using MediatR;

namespace EnterpriseFlow.Application.Features.Assistant.SendAssistantMessage;

/// <summary>HU-091/HU-092. No <c>IRequirePermission</c> — any authenticated user can talk to
/// the assistant; the security boundary lives in each individual tool it may invoke (ADR-0013),
/// not in a permission gate around the chat endpoint itself.</summary>
public sealed record SendAssistantMessageCommand(string Message) : IRequest<string>;
