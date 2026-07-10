namespace EnterpriseFlow.Application.Abstractions;

/// <summary>
/// The actual "send one email now" contract (F6.2). Distinct from <see cref="IEmailQueue"/>:
/// this is what a Hangfire job invokes when it runs, not what Application calls when a domain
/// event fires — Application only ever enqueues, it never sends synchronously (ADR-0011).
/// </summary>
public interface IEmailSender
{
    Task SendAsync(string recipientAddress, string subject, string body, CancellationToken cancellationToken);
}
