using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Infrastructure.Email;

public sealed class SmtpEmailService(
    ISmtpClient smtpClient,
    IOptions<EmailOptions> options,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task<bool> SendInvitationEmailAsync(
        string toEmail,
        string organizationName,
        string inviterName,
        UserRole role,
        string acceptUrl,
        DateTime expiresAt,
        CancellationToken cancellationToken)
    {
        var emailOptions = options.Value;

        using var message = new MailMessage
        {
            From = new MailAddress(emailOptions.FromAddress, emailOptions.FromName),
            Subject = $"You've been invited to join {organizationName} on TaskFlow",
            Body = BuildHtmlBody(organizationName, inviterName, role, acceptUrl, expiresAt),
            IsBodyHtml = true,
        };
        message.To.Add(toEmail);

        try
        {
            await smtpClient.SendMailAsync(message, cancellationToken);

            return true;
        }
        catch (SmtpException ex)
        {
            logger.LogWarning(ex, "SMTP invitation email to {Email} failed", toEmail);

            return false;
        }
    }

    private static string BuildHtmlBody(string organizationName, string inviterName, UserRole role, string acceptUrl, DateTime expiresAt) =>
        $"""
        <p>{inviterName} has invited you to join <strong>{organizationName}</strong> on TaskFlow as a {role}.</p>
        <p><a href="{acceptUrl}">Accept invitation</a></p>
        <p>This invitation expires on {expiresAt:MMMM d, yyyy 'at' h:mm tt} UTC.</p>
        """;
}
