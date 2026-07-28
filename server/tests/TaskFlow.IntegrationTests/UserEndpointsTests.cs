using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Auth.Dtos;
using TaskFlow.Application.Users.Dtos;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.IntegrationTests;

public class UserEndpointsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@acme.com";

    private async Task<RegisterResponse> RegisterAsync(HttpClient client, string organizationName, string email) =>
        (await (await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            OrganizationName: organizationName,
            Email: email,
            Password: "correct-horse-battery",
            FirstName: "Ada",
            LastName: "Lovelace"))).Content.ReadFromJsonAsync<RegisterResponse>())!;

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsCurrentUser()
    {
        var client = factory.CreateClient();
        var organizationName = $"Acme Inc {Guid.NewGuid():N}";
        var email = UniqueEmail();
        var registered = await RegisterAsync(client, organizationName, email);

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == registered.UserId);
            user.EmailConfirmed = true;
            await db.SaveChangesAsync();
        }

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "correct-horse-battery"));
        loginResponse.EnsureSuccessStatusCode();
        var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions))!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);

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
