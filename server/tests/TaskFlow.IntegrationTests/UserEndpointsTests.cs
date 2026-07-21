using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using TaskFlow.Application.Auth.Dtos;
using TaskFlow.Application.Users.Dtos;

namespace TaskFlow.IntegrationTests;

public class UserEndpointsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@acme.com";

    private async Task<RegisterResponse> RegisterAsync(HttpClient client, string organizationName) =>
        (await (await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            OrganizationName: organizationName,
            Email: UniqueEmail(),
            Password: "correct-horse-battery",
            FirstName: "Ada",
            LastName: "Lovelace"))).Content.ReadFromJsonAsync<RegisterResponse>())!;

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsCurrentUser()
    {
        var client = factory.CreateClient();
        var organizationName = $"Acme Inc {Guid.NewGuid():N}";
        var registered = await RegisterAsync(client, organizationName);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", registered.AccessToken);

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CurrentUserResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(registered.UserId, body!.Id);
        Assert.Equal(registered.OrganizationId, body.OrganizationId);
        Assert.Equal("Admin", body.Role.ToString());
        Assert.Equal(organizationName, body.OrganizationName);
    }

    [Fact]
    public async Task GetMe_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
