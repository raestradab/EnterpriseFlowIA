using EnterpriseFlow.Application.Abstractions;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace EnterpriseFlow.Application.Features.Assistant.GetAssistantMessages;

public sealed class GetAssistantMessagesQueryHandler(IAppDbContext db, ICurrentUserService currentUser)
    : IRequestHandler<GetAssistantMessagesQuery, IReadOnlyCollection<AssistantMessageDto>>
{
    public async Task<IReadOnlyCollection<AssistantMessageDto>> Handle(
        GetAssistantMessagesQuery request, CancellationToken cancellationToken)
    {
        // Sorted client-side, not via OrderBy in the query — SQLite (integration test suite)
        // can't translate ORDER BY over a DateTimeOffset column server-side, even though SQL
        // Server can; found the hard way with GetMyNotificationsQuery (Release 2, Sprint 9), not
        // repeated here.
        var messages = await db.AssistantMessages
            .Where(m => m.UserId == currentUser.UserId)
            .Select(m => new AssistantMessageDto(m.Id, m.Role, m.Content, m.CreatedAtUtc))
            .ToListAsync(cancellationToken);

        return messages.OrderBy(m => m.CreatedAtUtc).ToList();
    }
}
