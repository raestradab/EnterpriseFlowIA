namespace EnterpriseFlow.Infrastructure.Email;

/// <summary>Bound from the <c>Smtp</c> configuration section. Only read when Hangfire is also
/// configured — <see cref="SmtpEmailSender"/> is registered exclusively behind that same
/// condition (see DependencyInjection.cs).</summary>
public sealed class SmtpOptions
{
    public const string SectionName = "Smtp";

    public string Host { get; init; } = "localhost";

    public int Port { get; init; } = 25;

    public string? Username { get; init; }

    public string? Password { get; init; }

    public bool EnableSsl { get; init; } = true;

    public required string FromAddress { get; init; }
}
