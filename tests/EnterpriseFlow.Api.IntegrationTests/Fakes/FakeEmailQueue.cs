using EnterpriseFlow.Application.Abstractions;

namespace EnterpriseFlow.Api.IntegrationTests.Fakes;

/// <summary>Sprint 9 (Pruebas): replaces <c>NullEmailQueue</c>/<c>HangfireEmailQueue</c> in
/// tests — same reasoning as <see cref="FakeRealtimeNotifier"/>, records calls so a test can
/// assert the notification handler actually enqueues an email, not just that it compiles.</summary>
public sealed class FakeEmailQueue : IEmailQueue
{
    public List<(string RecipientAddress, string Subject, string Body)> Calls { get; } = [];

    public void Enqueue(string recipientAddress, string subject, string body) =>
        Calls.Add((recipientAddress, subject, body));
}
