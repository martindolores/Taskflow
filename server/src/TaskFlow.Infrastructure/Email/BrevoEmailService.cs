using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Infrastructure.Email;

public sealed class BrevoEmailService(
    HttpClient httpClient,
    IOptions<EmailOptions> options,
    ILogger<BrevoEmailService> logger) : IEmailService
{
    private const string EmailEndpoint = "https://api.brevo.com/v3/smtp/email";

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

        using var request = new HttpRequestMessage(HttpMethod.Post, EmailEndpoint)
        {
            Content = JsonContent.Create(new
            {
                sender = new { name = emailOptions.FromName, email = emailOptions.FromAddress },
                to = new[] { new { email = toEmail } },
                subject = $"You've been invited to join {organizationName} on TaskFlow",
                htmlContent = BuildHtmlBody(organizationName, inviterName, role, acceptUrl, expiresAt),
            }),
        };
        request.Headers.Add("api-key", emailOptions.Brevo.ApiKey ?? string.Empty);

        try
        {
            using var response = await httpClient.SendAsync(request, cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            logger.LogWarning(
                "Brevo invitation email to {Email} failed with status {StatusCode}: {Body}",
                toEmail, (int)response.StatusCode, body);

            return false;
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Brevo invitation email to {Email} failed", toEmail);

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
