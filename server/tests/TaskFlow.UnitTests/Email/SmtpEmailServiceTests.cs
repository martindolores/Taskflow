using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Email;

namespace TaskFlow.UnitTests.Email;

public class SmtpEmailServiceTests
{
    private static EmailOptions CreateOptions() => new()
    {
        Smtp = new SmtpOptions { Host = "smtp.gmail.com", Port = 587, Username = "taskflow@gmail.com", Password = "app-password" },
        FromAddress = "taskflow@gmail.com",
        FromName = "TaskFlow",
        FrontendBaseUrl = "https://app.taskflow.example",
    };

    private static SmtpEmailService CreateService(FakeSmtpClient client, out FakeLogger<SmtpEmailService> logger)
    {
        logger = new FakeLogger<SmtpEmailService>();
        return new SmtpEmailService(client, Options.Create(CreateOptions()), logger);
    }

    [Fact]
    public async Task SendInvitationEmailAsync_OnSuccess_SendsMailAndReturnsTrue()
    {
        var client = new FakeSmtpClient();
        var service = CreateService(client, out _);

        var result = await service.SendInvitationEmailAsync(
            "invitee@acme.com", "Acme Inc", "Ada Lovelace", UserRole.Member,
            "https://app.taskflow.example/accept-invitation?token=abc123", DateTime.UtcNow.AddDays(7),
            CancellationToken.None);

        Assert.True(result);
        Assert.NotNull(client.LastMessage);
        Assert.Equal("invitee@acme.com", client.LastMessage!.To.Single().Address);
        Assert.Equal("taskflow@gmail.com", client.LastMessage.From!.Address);
        Assert.Contains("Acme Inc", client.LastMessage.Body);
        Assert.Contains("https://app.taskflow.example/accept-invitation?token=abc123", client.LastMessage.Body);
    }

    [Fact]
    public async Task SendInvitationEmailAsync_OnSmtpFailure_ReturnsFalseWithoutThrowing()
    {
        var client = new FakeSmtpClient(shouldThrow: true);
        var service = CreateService(client, out var logger);

        var result = await service.SendInvitationEmailAsync(
            "invitee@acme.com", "Acme Inc", "Ada Lovelace", UserRole.Admin,
            "https://app.taskflow.example/accept-invitation?token=abc123", DateTime.UtcNow.AddDays(7),
            CancellationToken.None);

        Assert.False(result);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning);
    }

    private sealed class FakeSmtpClient(bool shouldThrow = false) : ISmtpClient
    {
        public MailMessage? LastMessage { get; private set; }

        public Task SendMailAsync(MailMessage message, CancellationToken cancellationToken)
        {
            LastMessage = message;
            if (shouldThrow)
            {
                throw new SmtpException("connection refused");
            }

            return Task.CompletedTask;
        }
    }
}
