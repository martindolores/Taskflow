using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TaskFlow.Application.Activity.Dtos;
using TaskFlow.Application.Auth.Dtos;
using TaskFlow.Application.Organizations.Dtos;
using TaskFlow.Application.TaskComments.Dtos;
using TaskFlow.Application.Tasks.Dtos;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.IntegrationTests;

public class ActivityEndpointsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
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

    private async Task<CreateTaskResponse> CreateTaskAsync(HttpClient client, string title = "Write docs")
    {
        var response = await client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest(title, null, TaskPriority.Medium, null, null));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreateTaskResponse>(JsonOptions))!;
    }

    [Fact]
    public async Task GetActivity_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/activity");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetActivity_AfterCreatingTask_ReturnsTaskCreatedEntry()
    {
        var admin = await RegisterAdminAsync();

        var task = await CreateTaskAsync(admin.Client, "Write docs");

        var response = await admin.Client.GetAsync("/api/activity");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<List<ActivityResponse>>(JsonOptions);
        Assert.NotNull(body);
        var entry = Assert.Single(body!, a => a.Type == ActivityType.TaskCreated && a.TaskId == task.Id);
        Assert.Equal("Ada Lovelace", entry.ActorName);
        Assert.Contains("Write docs", entry.Summary);
    }

    [Fact]
    public async Task GetActivity_AfterStatusChange_ReturnsTaskStatusChangedEntry()
    {
        var admin = await RegisterAdminAsync();
        var task = await CreateTaskAsync(admin.Client);

        var statusResponse = await admin.Client.PatchAsJsonAsync($"/api/tasks/{task.Id}/status", new UpdateTaskStatusRequest(TaskItemStatus.InProgress));
        statusResponse.EnsureSuccessStatusCode();

        var response = await admin.Client.GetAsync("/api/activity");
        var body = await response.Content.ReadFromJsonAsync<List<ActivityResponse>>(JsonOptions);

        Assert.Contains(body!, a => a.Type == ActivityType.TaskStatusChanged && a.TaskId == task.Id);
    }

    [Fact]
    public async Task GetActivity_AfterRedundantStatusChange_DoesNotDuplicateEntry()
    {
        var admin = await RegisterAdminAsync();
        var task = await CreateTaskAsync(admin.Client);

        var response = await admin.Client.PatchAsJsonAsync($"/api/tasks/{task.Id}/status", new UpdateTaskStatusRequest(TaskItemStatus.ToDo));
        response.EnsureSuccessStatusCode();

        var activityResponse = await admin.Client.GetAsync("/api/activity");
        var body = await activityResponse.Content.ReadFromJsonAsync<List<ActivityResponse>>(JsonOptions);

        Assert.DoesNotContain(body!, a => a.Type == ActivityType.TaskStatusChanged);
    }

    [Fact]
    public async Task GetActivity_AfterAssigningTaskViaUpdate_ReturnsTaskAssignedEntry()
    {
        var admin = await RegisterAdminAsync();
        var (_, memberId) = await InviteAndAcceptMemberAsync(admin.Client, UniqueEmail());
        var task = await CreateTaskAsync(admin.Client);

        var updateResponse = await admin.Client.PutAsJsonAsync($"/api/tasks/{task.Id}", new UpdateTaskRequest(
            task.Title, task.Description, task.Status, task.Priority, memberId, task.DueDate, task.ProjectId));
        updateResponse.EnsureSuccessStatusCode();

        var response = await admin.Client.GetAsync("/api/activity");
        var body = await response.Content.ReadFromJsonAsync<List<ActivityResponse>>(JsonOptions);

        Assert.Contains(body!, a => a.Type == ActivityType.TaskAssigned && a.TaskId == task.Id);
    }

    [Fact]
    public async Task GetActivity_AfterAddingComment_ReturnsCommentAddedEntry()
    {
        var admin = await RegisterAdminAsync();
        var task = await CreateTaskAsync(admin.Client);

        var commentResponse = await admin.Client.PostAsJsonAsync($"/api/tasks/{task.Id}/comments", new CreateCommentRequest("Looks good"));
        commentResponse.EnsureSuccessStatusCode();

        var response = await admin.Client.GetAsync("/api/activity");
        var body = await response.Content.ReadFromJsonAsync<List<ActivityResponse>>(JsonOptions);

        Assert.Contains(body!, a => a.Type == ActivityType.CommentAdded && a.TaskId == task.Id);
    }

    [Fact]
    public async Task GetActivity_AfterInvitingMember_ReturnsMemberInvitedEntryWithNullTaskId()
    {
        var admin = await RegisterAdminAsync();
        var email = UniqueEmail();

        var inviteResponse = await admin.Client.PostAsJsonAsync("/api/organization/invitations", new CreateInvitationRequest(email, UserRole.Member));
        inviteResponse.EnsureSuccessStatusCode();

        var response = await admin.Client.GetAsync("/api/activity");
        var body = await response.Content.ReadFromJsonAsync<List<ActivityResponse>>(JsonOptions);

        var entry = Assert.Single(body!, a => a.Type == ActivityType.MemberInvited);
        Assert.Null(entry.TaskId);
        Assert.Contains(email, entry.Summary);
    }

    [Fact]
    public async Task GetActivity_ReturnsMostRecentFirst()
    {
        var admin = await RegisterAdminAsync();
        var first = await CreateTaskAsync(admin.Client, "First task");
        var second = await CreateTaskAsync(admin.Client, "Second task");

        var response = await admin.Client.GetAsync("/api/activity");
        var body = await response.Content.ReadFromJsonAsync<List<ActivityResponse>>(JsonOptions);

        Assert.NotNull(body);
        var firstIndex = body!.FindIndex(a => a.TaskId == first.Id);
        var secondIndex = body.FindIndex(a => a.TaskId == second.Id);
        Assert.True(secondIndex < firstIndex);
    }

    [Fact]
    public async Task GetActivity_RespectsLimitParameter()
    {
        var admin = await RegisterAdminAsync();
        await CreateTaskAsync(admin.Client, "Task 1");
        await CreateTaskAsync(admin.Client, "Task 2");
        await CreateTaskAsync(admin.Client, "Task 3");

        var response = await admin.Client.GetAsync("/api/activity?limit=2");
        var body = await response.Content.ReadFromJsonAsync<List<ActivityResponse>>(JsonOptions);

        Assert.Equal(2, body!.Count);
    }

    [Fact]
    public async Task GetActivity_ReturnsOnlyEntriesInCurrentTenant()
    {
        var admin = await RegisterAdminAsync();
        await CreateTaskAsync(admin.Client, "Org A task");
        var otherAdmin = await RegisterAdminAsync();
        await CreateTaskAsync(otherAdmin.Client, "Org B task");

        var response = await admin.Client.GetAsync("/api/activity");
        var body = await response.Content.ReadFromJsonAsync<List<ActivityResponse>>(JsonOptions);

        Assert.NotNull(body);
        Assert.All(body!, a => Assert.NotEqual("Org B task", a.Summary));
    }
}
