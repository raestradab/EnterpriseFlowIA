namespace EnterpriseFlow.Application.Abstractions;

/// <summary>
/// F6.2 (ADR-0008/ADR-0011): Application calls this from a domain event handler to schedule an
/// email without blocking the request that triggered it — Infrastructure backs it with Hangfire
/// when configured, or a no-op when it isn't (same graceful-degradation shape as the Redis
/// fallback, see Infrastructure.DependencyInjection) so a missing mail setup never breaks the
/// request that raised the notification.
/// </summary>
public interface IEmailQueue
{
    void Enqueue(string recipientAddress, string subject, string body);
}
