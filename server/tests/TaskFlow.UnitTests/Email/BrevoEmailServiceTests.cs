using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Email;

namespace TaskFlow.UnitTests.Email;

public class BrevoEmailServiceTests
{
    private static EmailOptions CreateOptions() => new()
    {
        Brevo = new BrevoOptions { ApiKey = "test-api-key" },
        FromAddress = "notifications@taskflow.app",
        FromName = "TaskFlow",
        FrontendBaseUrl = "https://app.taskflow.example",
    };

    private static BrevoEmailService CreateService(StubHttpMessageHandler handler, out FakeLogger<BrevoEmailService> logger)
    {
        logger = new FakeLogger<BrevoEmailService>();
        var httpClient = new HttpClient(handler);
        return new BrevoEmailService(httpClient, Options.Create(CreateOptions()), logger);
    }

    [Fact]
    public async Task SendInvitationEmailAsync_OnSuccess_PostsToApiKeyEndpointAndReturnsTrue()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.Created));
        var service = CreateService(handler, out _);

        var result = await service.SendInvitationEmailAsync(
            "invitee@acme.com", "Acme Inc", "Ada Lovelace", UserRole.Member,
            "https://app.taskflow.example/accept-invitation?token=abc123", DateTime.UtcNow.AddDays(7),
            CancellationToken.None);

        Assert.True(result);
        Assert.NotNull(handler.LastRequest);
        Assert.Equal(HttpMethod.Post, handler.LastRequest!.Method);
        Assert.Equal("https://api.brevo.com/v3/smtp/email", handler.LastRequest.RequestUri!.ToString());
        Assert.Equal("test-api-key", handler.LastRequest.Headers.GetValues("api-key").Single());
        Assert.Contains("invitee@acme.com", handler.LastRequestBody);
        Assert.Contains("https://app.taskflow.example/accept-invitation?token=abc123", handler.LastRequestBody);
    }

    [Fact]
    public async Task SendInvitationEmailAsync_OnFailureStatusCode_ReturnsFalseWithoutThrowing()
    {
        var handler = new StubHttpMessageHandler(_ => new HttpResponseMessage(HttpStatusCode.BadRequest)
        {
            Content = new StringContent("{\"message\":\"invalid sender\"}"),
        });
        var service = CreateService(handler, out var logger);

        var result = await service.SendInvitationEmailAsync(
            "invitee@acme.com", "Acme Inc", "Ada Lovelace", UserRole.Admin,
            "https://app.taskflow.example/accept-invitation?token=abc123", DateTime.UtcNow.AddDays(7),
            CancellationToken.None);

        Assert.False(result);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning);
    }

    [Fact]
    public async Task SendInvitationEmailAsync_OnNetworkFailure_ReturnsFalseWithoutThrowing()
    {
        var handler = new StubHttpMessageHandler(_ => throw new HttpRequestException("connection refused"));
        var service = CreateService(handler, out var logger);

        var result = await service.SendInvitationEmailAsync(
            "invitee@acme.com", "Acme Inc", "Ada Lovelace", UserRole.Admin,
            "https://app.taskflow.example/accept-invitation?token=abc123", DateTime.UtcNow.AddDays(7),
            CancellationToken.None);

        Assert.False(result);
        Assert.Contains(logger.Entries, e => e.Level == LogLevel.Warning);
    }

    private sealed class StubHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder) : HttpMessageHandler
    {
        public HttpRequestMessage? LastRequest { get; private set; }

        public string? LastRequestBody { get; private set; }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            LastRequest = request;
            LastRequestBody = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);
            return responder(request);
        }
    }
}
