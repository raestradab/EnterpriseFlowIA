using EnterpriseFlow.Application.Abstractions;
using Hangfire;

namespace EnterpriseFlow.Infrastructure.Email;

/// <summary>F6.2/ADR-0008: enqueues onto the same Hangfire/SQL Server storage already wired for
/// background jobs — no separate queueing infrastructure. Only registered when Hangfire itself
/// is configured (see DependencyInjection.cs); <see cref="NullEmailQueue"/> covers the rest.</summary>
public sealed class HangfireEmailQueue(IBackgroundJobClient backgroundJobClient) : IEmailQueue
{
    public void Enqueue(string recipientAddress, string subject, string body) =>
        backgroundJobClient.Enqueue<IEmailSender>(sender => sender.SendAsync(recipientAddress, subject, body, CancellationToken.None));
}
