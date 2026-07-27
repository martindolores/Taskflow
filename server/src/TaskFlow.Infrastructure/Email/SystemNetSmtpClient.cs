using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Options;

namespace TaskFlow.Infrastructure.Email;

public sealed class SystemNetSmtpClient(IOptions<EmailOptions> options) : ISmtpClient
{
    public async Task SendMailAsync(MailMessage message, CancellationToken cancellationToken)
    {
        var smtpOptions = options.Value.Smtp;

        using var client = new SmtpClient(smtpOptions.Host, smtpOptions.Port)
        {
            EnableSsl = true,
            Credentials = new NetworkCredential(smtpOptions.Username, smtpOptions.Password),
        };

        await client.SendMailAsync(message, cancellationToken);
    }
}
