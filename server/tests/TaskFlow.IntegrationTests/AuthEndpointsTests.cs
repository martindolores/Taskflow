using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using TaskFlow.Application.Auth.Dtos;

namespace TaskFlow.IntegrationTests;

public class AuthEndpointsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@acme.com";

    private static RegisterRequest ValidRegisterRequest(string? email = null) => new(
        OrganizationName: "Acme Inc",
        Email: email ?? UniqueEmail(),
        Password: "correct-horse-battery",
        FirstName: "Ada",
        LastName: "Lovelace");

    private async Task<RegisterResponse> RegisterAsync(HttpClient client, string? email = null)
    {
        var response = await client.PostAsJsonAsync("/api/auth/register", ValidRegisterRequest(email));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<RegisterResponse>())!;
    }

    [Fact]
    public async Task Register_WithValidRequest_CreatesOrganizationAndReturnsTokens()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", ValidRegisterRequest());

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(Guid.Empty, body!.UserId);
        Assert.NotEqual(Guid.Empty, body.OrganizationId);
        Assert.False(string.IsNullOrWhiteSpace(body.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsConflict()
    {
        var client = factory.CreateClient();
        var email = UniqueEmail();
        await RegisterAsync(client, email);

        var response = await client.PostAsJsonAsync("/api/auth/register", ValidRegisterRequest(email));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithInvalidRequest_ReturnsBadRequest()
    {
        var client = factory.CreateClient();
        var request = ValidRegisterRequest() with { Email = "not-an-email" };

        var response = await client.PostAsJsonAsync("/api/auth/register", request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokensAndUser()
    {
        var client = factory.CreateClient();
        var email = UniqueEmail();
        await RegisterAsync(client, email);

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "correct-horse-battery"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.False(string.IsNullOrWhiteSpace(body!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(body.RefreshToken));
        Assert.Equal(email, body.User.Email);
        Assert.Equal("Admin", body.User.Role.ToString());
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();
        var email = UniqueEmail();
        await RegisterAsync(client, email);

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "wrong-password"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithUnknownEmail_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(UniqueEmail(), "whatever"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithValidToken_RotatesAndReturnsNewTokens()
    {
        var client = factory.CreateClient();
        var registered = await RegisterAsync(client);

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(registered.RefreshToken));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<RefreshTokenResponse>();
        Assert.NotNull(body);
        Assert.NotEqual(registered.RefreshToken, body!.RefreshToken);
        Assert.NotEqual(registered.AccessToken, body.AccessToken);
    }

    [Fact]
    public async Task Refresh_AfterRotation_RejectsOldToken()
    {
        var client = factory.CreateClient();
        var registered = await RegisterAsync(client);
        await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(registered.RefreshToken));

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(registered.RefreshToken));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Refresh_WithGarbageToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest("not-a-real-token"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_RevokesToken_SubsequentRefreshFails()
    {
        var client = factory.CreateClient();
        var registered = await RegisterAsync(client);

        var logoutResponse = await client.PostAsJsonAsync("/api/auth/logout", new LogoutRequest(registered.RefreshToken));
        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        var refreshResponse = await client.PostAsJsonAsync("/api/auth/refresh", new RefreshTokenRequest(registered.RefreshToken));
        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_WithUnknownToken_IsIdempotentNoContent()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/logout", new LogoutRequest("not-a-real-token"));

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }
}
