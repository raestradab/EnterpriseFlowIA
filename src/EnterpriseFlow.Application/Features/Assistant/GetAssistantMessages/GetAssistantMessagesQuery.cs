using EnterpriseFlow.Domain.Enums;
using MediatR;

namespace EnterpriseFlow.Application.Features.Assistant.GetAssistantMessages;

/// <summary>HU-091. No <c>IRequirePermission</c> — always scoped to the caller's own messages
/// (<c>ICurrentUserService.UserId</c> in the handler), same reasoning as
/// <c>GetMyNotificationsQuery</c> (Release 2).</summary>
public sealed record GetAssistantMessagesQuery : IRequest<IReadOnlyCollection<AssistantMessageDto>>;

public sealed record AssistantMessageDto(Guid Id, AssistantMessageRole Role, string Content, DateTimeOffset CreatedAtUtc);
