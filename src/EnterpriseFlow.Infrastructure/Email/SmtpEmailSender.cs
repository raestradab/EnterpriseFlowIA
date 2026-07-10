using System.Net;
using System.Net.Mail;
using EnterpriseFlow.Application.Abstractions;
using Microsoft.Extensions.Options;

namespace EnterpriseFlow.Infrastructure.Email;

/// <summary>
/// F6.2. Built on <see cref="System.Net.Mail.SmtpClient"/> — Microsoft's own docs recommend a
/// third-party library (e.g. MailKit) for new production code, but this project has no SMTP
/// server to actually exercise either way (same "not runtime-verified in this environment" gap
/// as the cloud Document storage providers, see r2-07b-backend-documentos.md); pulling in an
/// extra untested dependency for that wouldn't buy anything real. This is what a Hangfire job
/// invokes when it runs — Application only ever reaches <see cref="IEmailQueue"/>, never this
/// type directly (ADR-0011).
/// </summary>
public sealed class SmtpEmailSender(IOptions<SmtpOptions> options) : IEmailSender
{
    public async Task SendAsync(string recipientAddress, string subject, string body, CancellationToken cancellationToken)
    {
        var settings = options.Value;

        using var client = new SmtpClient(settings.Host, settings.Port) { EnableSsl = settings.EnableSsl };
        if (!string.IsNullOrWhiteSpace(settings.Username))
        {
            client.Credentials = new NetworkCredential(settings.Username, settings.Password);
        }

        using var message = new MailMessage(settings.FromAddress, recipientAddress, subject, body);
        await client.SendMailAsync(message, cancellationToken);
    }
}
