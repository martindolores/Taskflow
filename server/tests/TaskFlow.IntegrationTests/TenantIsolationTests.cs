using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TaskFlow.Application.Auth.Dtos;
using TaskFlow.Application.Common;
using TaskFlow.Application.Organizations.Dtos;
using TaskFlow.Application.Tasks.Dtos;
using TaskFlow.Application.TaskComments.Dtos;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.IntegrationTests;

public class TenantIsolationTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private sealed class FixedTenantService(Guid? organizationId) : ICurrentTenantService
    {
        public Guid? OrganizationId => organizationId;
    }

    private sealed record Organization(Guid Id, HttpClient Client);

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

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

    private async Task<Organization> RegisterOrganizationWithClientAsync()
    {
        var client = factory.CreateClient();
        var email = UniqueEmail();
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            OrganizationName: $"Acme Inc {Guid.NewGuid():N}",
            Email: email,
            Password: "correct-horse-battery",
            FirstName: "Ada",
            LastName: "Lovelace"));
        response.EnsureSuccessStatusCode();
        var body = (await response.Content.ReadFromJsonAsync<RegisterResponse>())!;

        using (var scope = factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var user = await db.Users.IgnoreQueryFilters().SingleAsync(u => u.Id == body.UserId);
            user.EmailConfirmed = true;
            await db.SaveChangesAsync();
        }

        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "correct-horse-battery"));
        loginResponse.EnsureSuccessStatusCode();
        var login = (await loginResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions))!;

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return new Organization(body.OrganizationId, client);
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

    [Fact]
    public async Task Tasks_QueryFilter_OnlyReturnsTasksInCurrentTenant()
    {
        var orgA = await RegisterOrganizationWithClientAsync();
        var orgB = await RegisterOrganizationWithClientAsync();

        var createA = await orgA.Client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest("Org A task", null, TaskPriority.Medium, null, null));
        createA.EnsureSuccessStatusCode();
        var createB = await orgB.Client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest("Org B task", null, TaskPriority.Medium, null, null));
        createB.EnsureSuccessStatusCode();

        await using var db = CreateScopedDbContext(orgA.Id);
        var tasks = await db.Tasks.ToListAsync();

        Assert.NotEmpty(tasks);
        Assert.All(tasks, t => Assert.Equal(orgA.Id, t.OrganizationId));
        Assert.DoesNotContain(tasks, t => t.OrganizationId == orgB.Id);
    }

    [Fact]
    public async Task TaskComments_QueryFilter_OnlyReturnsCommentsInCurrentTenant()
    {
        var orgA = await RegisterOrganizationWithClientAsync();
        var orgB = await RegisterOrganizationWithClientAsync();

        var taskAResponse = await orgA.Client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest("Org A task", null, TaskPriority.Medium, null, null));
        var taskA = (await taskAResponse.Content.ReadFromJsonAsync<CreateTaskResponse>(JsonOptions))!;
        var taskBResponse = await orgB.Client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest("Org B task", null, TaskPriority.Medium, null, null));
        var taskB = (await taskBResponse.Content.ReadFromJsonAsync<CreateTaskResponse>(JsonOptions))!;

        var commentA = await orgA.Client.PostAsJsonAsync($"/api/tasks/{taskA.Id}/comments", new CreateCommentRequest("Comment on A"));
        commentA.EnsureSuccessStatusCode();
        var commentB = await orgB.Client.PostAsJsonAsync($"/api/tasks/{taskB.Id}/comments", new CreateCommentRequest("Comment on B"));
        commentB.EnsureSuccessStatusCode();

        await using var db = CreateScopedDbContext(orgA.Id);
        var comments = await db.TaskComments.ToListAsync();

        Assert.NotEmpty(comments);
        Assert.All(comments, c => Assert.Equal(orgA.Id, c.OrganizationId));
        Assert.DoesNotContain(comments, c => c.OrganizationId == orgB.Id);
    }

    [Fact]
    public async Task Invitations_QueryFilter_OnlyReturnsInvitationsInCurrentTenant()
    {
        var orgA = await RegisterOrganizationWithClientAsync();
        var orgB = await RegisterOrganizationWithClientAsync();

        var inviteA = await orgA.Client.PostAsJsonAsync("/api/organization/invitations", new CreateInvitationRequest(UniqueEmail(), UserRole.Member));
        inviteA.EnsureSuccessStatusCode();
        var inviteB = await orgB.Client.PostAsJsonAsync("/api/organization/invitations", new CreateInvitationRequest(UniqueEmail(), UserRole.Member));
        inviteB.EnsureSuccessStatusCode();

        await using var db = CreateScopedDbContext(orgA.Id);
        var invitations = await db.Invitations.ToListAsync();

        Assert.NotEmpty(invitations);
        Assert.All(invitations, i => Assert.Equal(orgA.Id, i.OrganizationId));
        Assert.DoesNotContain(invitations, i => i.OrganizationId == orgB.Id);
    }

    [Fact]
    public async Task ActivityLog_QueryFilter_OnlyReturnsEntriesInCurrentTenant()
    {
        var orgA = await RegisterOrganizationWithClientAsync();
        var orgB = await RegisterOrganizationWithClientAsync();

        var createA = await orgA.Client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest("Org A task", null, TaskPriority.Medium, null, null));
        createA.EnsureSuccessStatusCode();
        var createB = await orgB.Client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest("Org B task", null, TaskPriority.Medium, null, null));
        createB.EnsureSuccessStatusCode();

        await using var db = CreateScopedDbContext(orgA.Id);
        var entries = await db.ActivityLog.ToListAsync();

        Assert.NotEmpty(entries);
        Assert.All(entries, e => Assert.Equal(orgA.Id, e.OrganizationId));
        Assert.DoesNotContain(entries, e => e.OrganizationId == orgB.Id);
    }
}
