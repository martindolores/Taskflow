using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Auth.Dtos;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.IntegrationTests;

public class RateLimitingTests
{
    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@acme.com";

    private static RegisterRequest ValidRegisterRequest() => new(
        OrganizationName: $"Acme Inc {Guid.NewGuid():N}",
        Email: UniqueEmail(),
        Password: "correct-horse-battery",
        FirstName: "Ada",
        LastName: "Lovelace");

    private static WebApplicationFactory<Program> CreateFactory(int permitLimit, int windowMinutes) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["RateLimiting:Register:PermitLimit"] = permitLimit.ToString(),
                ["RateLimiting:Register:WindowMinutes"] = windowMinutes.ToString(),
            })));

    [Fact]
    public async Task Register_WithinLimit_AllSucceed()
    {
        using var factory = CreateFactory(permitLimit: 3, windowMinutes: 10);
        var client = factory.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/register", ValidRegisterRequest());
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }
    }

    [Fact]
    public async Task Register_ExceedingPerIpLimit_ReturnsTooManyRequestsWithRetryAfter()
    {
        using var factory = CreateFactory(permitLimit: 3, windowMinutes: 10);
        var client = factory.CreateClient();

        for (var i = 0; i < 3; i++)
        {
            var response = await client.PostAsJsonAsync("/api/auth/register", ValidRegisterRequest());
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        var rejected = await client.PostAsJsonAsync("/api/auth/register", ValidRegisterRequest());

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
        Assert.True(rejected.Headers.Contains("Retry-After"));
    }

    [Fact]
    public async Task Login_IsNotRateLimited_EvenAfterExceedingRegisterLimit()
    {
        using var factory = CreateFactory(permitLimit: 1, windowMinutes: 10);
        var client = factory.CreateClient();
        var email = UniqueEmail();
        var registerResponse = await client.PostAsJsonAsync("/api/auth/register", ValidRegisterRequest() with { Email = email });
        Assert.Equal(HttpStatusCode.Created, registerResponse.StatusCode);
        var registered = (await registerResponse.Content.ReadFromJsonAsync<RegisterResponse>())!;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == registered.UserId);
            user.EmailConfirmed = true;
            await db.SaveChangesAsync();
        }

        for (var i = 0; i < 5; i++)
        {
            var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "correct-horse-battery"));
            Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        }
    }
}
