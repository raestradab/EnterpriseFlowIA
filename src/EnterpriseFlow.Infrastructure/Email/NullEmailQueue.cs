using EnterpriseFlow.Application.Abstractions;

namespace EnterpriseFlow.Infrastructure.Email;

/// <summary>
/// Graceful degradation when Hangfire isn't configured (local dev without a Hangfire connection
/// string, or the "Testing" environment) — same shape as the Redis-to-in-memory-cache fallback
/// in DependencyInjection.cs. A missing mail setup must never break the request that raised a
/// notification (ADR-0011); it just means no email goes out.
/// </summary>
public sealed class NullEmailQueue : IEmailQueue
{
    public void Enqueue(string recipientAddress, string subject, string body)
    {
    }
}
