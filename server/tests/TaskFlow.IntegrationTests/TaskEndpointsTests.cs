using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Auth.Dtos;
using TaskFlow.Application.Common;
using TaskFlow.Application.Organizations.Dtos;
using TaskFlow.Application.Tasks.Dtos;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.IntegrationTests;

public class TaskEndpointsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
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

    private async Task<CreateTaskResponse> CreateTaskAsync(HttpClient client, string title = "Write docs", TaskPriority priority = TaskPriority.Medium, Guid? assigneeId = null)
    {
        var response = await client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest(title, "Some description", priority, assigneeId, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateTaskResponse>(JsonOptions))!;
    }

    [Fact]
    public async Task CreateTask_WithValidRequest_ReturnsCreatedTask()
    {
        var admin = await RegisterAdminAsync();

        var response = await admin.Client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest("Write docs", "Body", TaskPriority.High, null, null));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CreateTaskResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Write docs", body!.Title);
        Assert.Equal(TaskPriority.High, body.Priority);
        Assert.Equal(TaskItemStatus.ToDo, body.Status);
    }

    [Fact]
    public async Task CreateTask_WithBlankTitle_ReturnsBadRequest()
    {
        var admin = await RegisterAdminAsync();

        var response = await admin.Client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest("", null, TaskPriority.Medium, null, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest("Write docs", null, TaskPriority.Medium, null, null));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateTask_WithAssigneeFromAnotherOrg_ReturnsBadRequest()
    {
        var admin = await RegisterAdminAsync();
        var otherAdmin = await RegisterAdminAsync();

        var response = await admin.Client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest("Write docs", null, TaskPriority.Medium, otherAdmin.Registered.UserId, null));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetTask_ReturnsTaskDetail()
    {
        var admin = await RegisterAdminAsync();
        var created = await CreateTaskAsync(admin.Client);

        var response = await admin.Client.GetAsync($"/api/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TaskResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(created.Id, body!.Id);
        Assert.Equal(admin.Registered.UserId, body.CreatedById);
    }

    [Fact]
    public async Task GetTask_FromAnotherOrg_ReturnsNotFound()
    {
        var admin = await RegisterAdminAsync();
        var created = await CreateTaskAsync(admin.Client);
        var otherAdmin = await RegisterAdminAsync();

        var response = await otherAdmin.Client.GetAsync($"/api/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTask_UnknownId_ReturnsNotFound()
    {
        var admin = await RegisterAdminAsync();

        var response = await admin.Client.GetAsync($"/api/tasks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetTasks_ReturnsOnlyTasksInCurrentTenant()
    {
        var admin = await RegisterAdminAsync();
        await CreateTaskAsync(admin.Client, "Task A");
        var otherAdmin = await RegisterAdminAsync();
        await CreateTaskAsync(otherAdmin.Client, "Task B");

        var response = await admin.Client.GetAsync("/api/tasks");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<PagedResult<TaskListItemResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Single(body!.Items);
        Assert.Equal("Task A", body.Items[0].Title);
        Assert.Equal(1, body.Total);
    }

    [Fact]
    public async Task GetTasks_FiltersByStatusAndPriority()
    {
        var admin = await RegisterAdminAsync();
        var lowPriority = await CreateTaskAsync(admin.Client, "Low priority task", TaskPriority.Low);
        await CreateTaskAsync(admin.Client, "High priority task", TaskPriority.High);
        await admin.Client.PatchAsJsonAsync($"/api/tasks/{lowPriority.Id}/status", new UpdateTaskStatusRequest(TaskItemStatus.Done));

        var response = await admin.Client.GetAsync("/api/tasks?priority=Low&status=Done");

        var body = await response.Content.ReadFromJsonAsync<PagedResult<TaskListItemResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Single(body!.Items);
        Assert.Equal(lowPriority.Id, body.Items[0].Id);
    }

    [Fact]
    public async Task GetTasks_PaginatesResults()
    {
        var admin = await RegisterAdminAsync();
        for (var i = 0; i < 3; i++)
        {
            await CreateTaskAsync(admin.Client, $"Task {i}");
        }

        var response = await admin.Client.GetAsync("/api/tasks?page=1&pageSize=2");

        var body = await response.Content.ReadFromJsonAsync<PagedResult<TaskListItemResponse>>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(2, body!.Items.Count);
        Assert.Equal(3, body.Total);
        Assert.Equal(1, body.Page);
        Assert.Equal(2, body.PageSize);
    }

    [Fact]
    public async Task UpdateTask_WithValidRequest_UpdatesFields()
    {
        var admin = await RegisterAdminAsync();
        var created = await CreateTaskAsync(admin.Client);

        var response = await admin.Client.PutAsJsonAsync($"/api/tasks/{created.Id}", new UpdateTaskRequest(
            "Updated title", "Updated body", TaskItemStatus.InProgress, TaskPriority.Low, null, null));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TaskResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Updated title", body!.Title);
        Assert.Equal(TaskItemStatus.InProgress, body.Status);
        Assert.Equal(TaskPriority.Low, body.Priority);
    }

    [Fact]
    public async Task UpdateTask_UnknownId_ReturnsNotFound()
    {
        var admin = await RegisterAdminAsync();

        var response = await admin.Client.PutAsJsonAsync($"/api/tasks/{Guid.NewGuid()}", new UpdateTaskRequest(
            "Title", null, TaskItemStatus.ToDo, TaskPriority.Medium, null, null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTask_FromAnotherOrg_ReturnsNotFound()
    {
        var admin = await RegisterAdminAsync();
        var created = await CreateTaskAsync(admin.Client);
        var otherAdmin = await RegisterAdminAsync();

        var response = await otherAdmin.Client.PutAsJsonAsync($"/api/tasks/{created.Id}", new UpdateTaskRequest(
            "Updated title", "Updated body", TaskItemStatus.InProgress, TaskPriority.Low, null, null));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateTaskStatus_WithValidRequest_UpdatesStatus()
    {
        var admin = await RegisterAdminAsync();
        var created = await CreateTaskAsync(admin.Client);

        var response = await admin.Client.PatchAsJsonAsync($"/api/tasks/{created.Id}/status", new UpdateTaskStatusRequest(TaskItemStatus.Done));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<TaskStatusResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(TaskItemStatus.Done, body!.Status);
    }

    [Fact]
    public async Task UpdateTaskStatus_FromAnotherOrg_ReturnsNotFound()
    {
        var admin = await RegisterAdminAsync();
        var created = await CreateTaskAsync(admin.Client);
        var otherAdmin = await RegisterAdminAsync();

        var response = await otherAdmin.Client.PatchAsJsonAsync($"/api/tasks/{created.Id}/status", new UpdateTaskStatusRequest(TaskItemStatus.Done));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTask_AsCreator_RemovesTask()
    {
        var admin = await RegisterAdminAsync();
        var created = await CreateTaskAsync(admin.Client);

        var response = await admin.Client.DeleteAsync($"/api/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var getResponse = await admin.Client.GetAsync($"/api/tasks/{created.Id}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteTask_AsAdminNonCreator_RemovesTask()
    {
        var admin = await RegisterAdminAsync();
        var (memberClient, _) = await InviteAndAcceptMemberAsync(admin.Client, UniqueEmail());
        var created = await CreateTaskAsync(memberClient);

        var response = await admin.Client.DeleteAsync($"/api/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTask_AsNonCreatorNonAdmin_ReturnsForbidden()
    {
        var admin = await RegisterAdminAsync();
        var created = await CreateTaskAsync(admin.Client);
        var (memberClient, _) = await InviteAndAcceptMemberAsync(admin.Client, UniqueEmail());

        var response = await memberClient.DeleteAsync($"/api/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTask_UnknownId_ReturnsNotFound()
    {
        var admin = await RegisterAdminAsync();

        var response = await admin.Client.DeleteAsync($"/api/tasks/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteTask_FromAnotherOrg_ReturnsNotFound()
    {
        var admin = await RegisterAdminAsync();
        var created = await CreateTaskAsync(admin.Client);
        var otherAdmin = await RegisterAdminAsync();

        var response = await otherAdmin.Client.DeleteAsync($"/api/tasks/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
