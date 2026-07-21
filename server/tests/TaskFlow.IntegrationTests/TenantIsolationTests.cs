using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskFlow.Application.Auth.Dtos;
using TaskFlow.Application.Common;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.IntegrationTests;

public class TenantIsolationTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private sealed class FixedTenantService(Guid? organizationId) : ICurrentTenantService
    {
        public Guid? OrganizationId => organizationId;
    }

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@acme.com";

    private async Task<Guid> RegisterOrganizationAsync()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            OrganizationName: $"Acme Inc {Guid.NewGuid():N}",
            Email: UniqueEmail(),
            Password: "correct-horse-battery",
            FirstName: "Ada",
            LastName: "Lovelace"));
        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<RegisterResponse>();
        return body!.OrganizationId;
    }

    private AppDbContext CreateScopedDbContext(Guid? organizationId)
    {
        var scopedFactory = factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<ICurrentTenantService>();
            services.AddScoped<ICurrentTenantService>(_ => new FixedTenantService(organizationId));
        }));

        return scopedFactory.Services.CreateScope().ServiceProvider.GetRequiredService<AppDbContext>();
    }

    [Fact]
    public async Task Users_QueryFilter_OnlyReturnsUsersInCurrentTenant()
    {
        var orgA = await RegisterOrganizationAsync();
        var orgB = await RegisterOrganizationAsync();

        await using var db = CreateScopedDbContext(orgA);
        var users = await db.Users.ToListAsync();

        Assert.NotEmpty(users);
        Assert.All(users, u => Assert.Equal(orgA, u.OrganizationId));
        Assert.DoesNotContain(users, u => u.OrganizationId == orgB);
    }

    [Fact]
    public async Task Users_QueryFilter_WithNoTenantContext_ReturnsNoRows()
    {
        await RegisterOrganizationAsync();

        await using var db = CreateScopedDbContext(null);
        var users = await db.Users.ToListAsync();

        Assert.Empty(users);
    }
}
