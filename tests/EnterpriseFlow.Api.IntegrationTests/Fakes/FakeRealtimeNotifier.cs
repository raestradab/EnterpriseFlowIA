using EnterpriseFlow.Application.Abstractions;

namespace EnterpriseFlow.Api.IntegrationTests.Fakes;

/// <summary>
/// Sprint 9 (Pruebas): replaces <c>SignalRNotifier</c> in tests (see
/// <c>CustomWebApplicationFactory</c>) — the real one needs a connected SignalR client to
/// observe anything, which the integration test host doesn't have. Records calls instead, so a
/// test can assert <see cref="NotifyOnDocumentWorkflowTransitionedHandler"/> actually invokes
/// <see cref="IRealtimeNotifier"/> with the right arguments, not just that a Notification row
/// got persisted (the gap coverage measurement found: 0% of this call was ever exercised).
/// </summary>
public sealed class FakeRealtimeNotifier : IRealtimeNotifier
{
    public List<(Guid UserId, string EventName, object Payload)> Calls { get; } = [];

    public Task NotifyUserAsync(Guid userId, string eventName, object payload, CancellationToken cancellationToken)
    {
        Calls.Add((userId, eventName, payload));
        return Task.CompletedTask;
    }
}
