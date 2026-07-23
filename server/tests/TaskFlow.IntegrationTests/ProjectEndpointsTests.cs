using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Auth.Dtos;
using TaskFlow.Application.Organizations.Dtos;
using TaskFlow.Application.Projects.Dtos;
using TaskFlow.Application.Tasks.Dtos;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.IntegrationTests;

public class ProjectEndpointsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() },
    };

    private static string UniqueEmail() => $"user-{Guid.NewGuid():N}@acme.com";

    private sealed record AdminContext(HttpClient Client, RegisterResponse Registered);

    private async Task<AdminContext> RegisterAdminAsync()
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/register", new RegisterRequest(
            OrganizationName: $"Acme Inc {Guid.NewGuid():N}",
            Email: UniqueEmail(),
            Password: "correct-horse-battery",
            FirstName: "Ada",
            LastName: "Lovelace"));
        response.EnsureSuccessStatusCode();
        var body = (await response.Content.ReadFromJsonAsync<RegisterResponse>())!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);
        return new AdminContext(client, body);
    }

    private async Task<(HttpClient Client, Guid UserId)> InviteAndAcceptMemberAsync(HttpClient adminClient, string email)
    {
        var inviteResponse = await adminClient.PostAsJsonAsync("/api/organization/invitations", new CreateInvitationRequest(email, UserRole.Member));
        inviteResponse.EnsureSuccessStatusCode();
        var invitation = (await inviteResponse.Content.ReadFromJsonAsync<InvitationResponse>(JsonOptions))!;

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var token = (await db.Invitations.IgnoreQueryFilters().SingleAsync(i => i.Id == invitation.Id)).Token;

        var client = factory.CreateClient();
        var acceptResponse = await client.PostAsJsonAsync("/api/auth/accept-invitation", new AcceptInvitationRequest(token, "correct-horse-battery", "New", "Hire"));
        acceptResponse.EnsureSuccessStatusCode();
        var body = (await acceptResponse.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions))!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);
        return (client, body.User.Id);
    }

    private async Task<ProjectResponse> CreateProjectAsync(HttpClient client, string name = "Marketing", string color = "#FF5733")
    {
        var response = await client.PostAsJsonAsync("/api/projects", new CreateProjectRequest(name, color));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<ProjectResponse>(JsonOptions))!;
    }

    [Fact]
    public async Task CreateProject_AsAdmin_ReturnsCreatedProject()
    {
        var admin = await RegisterAdminAsync();

        var response = await admin.Client.PostAsJsonAsync("/api/projects", new CreateProjectRequest("Marketing", "#FF5733"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ProjectResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Marketing", body!.Name);
        Assert.Equal("#FF5733", body.Color);
    }

    [Fact]
    public async Task CreateProject_AsMember_ReturnsForbidden()
    {
        var admin = await RegisterAdminAsync();
        var (memberClient, _) = await InviteAndAcceptMemberAsync(admin.Client, UniqueEmail());

        var response = await memberClient.PostAsJsonAsync("/api/projects", new CreateProjectRequest("Marketing", "#FF5733"));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithBlankName_ReturnsBadRequest()
    {
        var admin = await RegisterAdminAsync();

        var response = await admin.Client.PostAsJsonAsync("/api/projects", new CreateProjectRequest("", "#FF5733"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithInvalidColor_ReturnsBadRequest()
    {
        var admin = await RegisterAdminAsync();

        var response = await admin.Client.PostAsJsonAsync("/api/projects", new CreateProjectRequest("Marketing", "not-a-color"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/projects", new CreateProjectRequest("Marketing", "#FF5733"));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProjects_ReturnsOnlyProjectsInCurrentTenant()
    {
        var admin = await RegisterAdminAsync();
        await CreateProjectAsync(admin.Client, "Marketing");
        var otherAdmin = await RegisterAdminAsync();
        await CreateProjectAsync(otherAdmin.Client, "Engineering");

        var response = await admin.Client.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<ProjectResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Single(body!);
        Assert.Equal("Marketing", body[0].Name);
    }

    [Fact]
    public async Task DeleteProject_AsAdmin_RemovesProject()
    {
        var admin = await RegisterAdminAsync();
        var created = await CreateProjectAsync(admin.Client);

        var response = await admin.Client.DeleteAsync($"/api/projects/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var listResponse = await admin.Client.GetAsync("/api/projects");
        var body = await listResponse.Content.ReadFromJsonAsync<List<ProjectResponse>>(JsonOptions);
        Assert.Empty(body!);
    }

    [Fact]
    public async Task DeleteProject_AsMember_ReturnsForbidden()
    {
        var admin = await RegisterAdminAsync();
        var created = await CreateProjectAsync(admin.Client);
        var (memberClient, _) = await InviteAndAcceptMemberAsync(admin.Client, UniqueEmail());

        var response = await memberClient.DeleteAsync($"/api/projects/{created.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_UnknownId_ReturnsNotFound()
    {
        var admin = await RegisterAdminAsync();

        var response = await admin.Client.DeleteAsync($"/api/projects/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_FromAnotherOrg_ReturnsNotFound()
    {
        var admin = await RegisterAdminAsync();
        var created = await CreateProjectAsync(admin.Client);
        var otherAdmin = await RegisterAdminAsync();

        var response = await otherAdmin.Client.DeleteAsync($"/api/projects/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_WithTasksAssigned_TasksFallBackToNullProject()
    {
        var admin = await RegisterAdminAsync();
        var project = await CreateProjectAsync(admin.Client);
        var taskResponse = await admin.Client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest(
            "Write docs", null, TaskPriority.Medium, null, null, project.Id));
        taskResponse.EnsureSuccessStatusCode();
        var task = (await taskResponse.Content.ReadFromJsonAsync<CreateTaskResponse>(JsonOptions))!;

        var deleteResponse = await admin.Client.DeleteAsync($"/api/projects/{project.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getTaskResponse = await admin.Client.GetAsync($"/api/tasks/{task.Id}");
        var updatedTask = await getTaskResponse.Content.ReadFromJsonAsync<TaskResponse>(JsonOptions);
        Assert.Null(updatedTask!.ProjectId);
    }
}
