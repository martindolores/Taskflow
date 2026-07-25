using Microsoft.Extensions.Logging;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Infrastructure.Email;

public sealed class NullEmailService(ILogger<NullEmailService> logger) : IEmailService
{
    public Task<bool> SendInvitationEmailAsync(
        string toEmail,
        string organizationName,
        string inviterName,
        UserRole role,
        string acceptUrl,
        DateTime expiresAt,
        CancellationToken cancellationToken)
    {
        logger.LogInformation(
            "Email sending is disabled — invitation for {Email} to join {Organization} as {Role} would link to {AcceptUrl} (expires {ExpiresAt})",
            toEmail, organizationName, role, acceptUrl, expiresAt);

        return Task.FromResult(true);
    }
}
