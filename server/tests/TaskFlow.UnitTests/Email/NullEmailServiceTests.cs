using Microsoft.Extensions.Logging;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Email;

namespace TaskFlow.UnitTests.Email;

public class NullEmailServiceTests
{
    [Fact]
    public async Task SendInvitationEmailAsync_ReturnsTrue()
    {
        var service = new NullEmailService(new FakeLogger<NullEmailService>());

        var result = await service.SendInvitationEmailAsync(
            "invitee@acme.com", "Acme Inc", "Ada Lovelace", UserRole.Member,
            "https://app.taskflow.example/accept-invitation?token=abc123", DateTime.UtcNow.AddDays(7),
            CancellationToken.None);

        Assert.True(result);
    }

    [Fact]
    public async Task SendInvitationEmailAsync_LogsInvitationDetailsInsteadOfSending()
    {
        var logger = new FakeLogger<NullEmailService>();
        var service = new NullEmailService(logger);

        await service.SendInvitationEmailAsync(
            "invitee@acme.com", "Acme Inc", "Ada Lovelace", UserRole.Member,
            "https://app.taskflow.example/accept-invitation?token=abc123", DateTime.UtcNow.AddDays(7),
            CancellationToken.None);

        var entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Contains("invitee@acme.com", entry.Message);
        Assert.Contains("https://app.taskflow.example/accept-invitation?token=abc123", entry.Message);
    }
}
