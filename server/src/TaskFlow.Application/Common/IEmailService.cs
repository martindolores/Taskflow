using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Common;

public interface IEmailService
{
    Task<bool> SendInvitationEmailAsync(
        string toEmail,
        string organizationName,
        string inviterName,
        UserRole role,
        string acceptUrl,
        DateTime expiresAt,
        CancellationToken cancellationToken);
}
