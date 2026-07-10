namespace EnterpriseFlow.Domain.Enums;

/// <summary>Only the two turns worth persisting long-term (HU-091) — the transient System
/// prompt and Tool call/result messages a request needs to reach an answer are reconstructed
/// fresh each time (Application.Abstractions.AiChatRole), not part of what a user actually
/// asked or was told.</summary>
public enum AssistantMessageRole
{
    User = 0,
    Assistant = 1,
}
