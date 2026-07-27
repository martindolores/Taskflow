using System.Net.Mail;

namespace TaskFlow.Infrastructure.Email;

public interface ISmtpClient
{
    Task SendMailAsync(MailMessage message, CancellationToken cancellationToken);
}
