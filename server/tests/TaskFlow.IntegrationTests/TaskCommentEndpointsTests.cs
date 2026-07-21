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
using TaskFlow.Application.TaskComments.Dtos;
using TaskFlow.Application.Tasks.Dtos;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.IntegrationTests;

public class TaskCommentEndpointsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
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

    private async Task<Guid> CreateTaskAsync(HttpClient client, string title = "Write docs")
    {
        var response = await client.PostAsJsonAsync("/api/tasks", new CreateTaskRequest(title, null, TaskPriority.Medium, null, null));
        response.EnsureSuccessStatusCode();
        var body = (await response.Content.ReadFromJsonAsync<CreateTaskResponse>(JsonOptions))!;
        return body.Id;
    }

    [Fact]
    public async Task CreateComment_WithValidBody_ReturnsCreatedComment()
    {
        var admin = await RegisterAdminAsync();
        var taskId = await CreateTaskAsync(admin.Client);

        var response = await admin.Client.PostAsJsonAsync($"/api/tasks/{taskId}/comments", new CreateCommentRequest("Looks good."));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<CommentResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal("Looks good.", body!.Body);
        Assert.Equal(admin.Registered.UserId, body.AuthorId);
        Assert.Equal("Ada Lovelace", body.AuthorName);
    }

    [Fact]
    public async Task CreateComment_WithBlankBody_ReturnsBadRequest()
    {
        var admin = await RegisterAdminAsync();
        var taskId = await CreateTaskAsync(admin.Client);

        var response = await admin.Client.PostAsJsonAsync($"/api/tasks/{taskId}/comments", new CreateCommentRequest(""));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateComment_OnTaskFromAnotherOrg_ReturnsNotFound()
    {
        var admin = await RegisterAdminAsync();
        var taskId = await CreateTaskAsync(admin.Client);
        var otherAdmin = await RegisterAdminAsync();

        var response = await otherAdmin.Client.PostAsJsonAsync($"/api/tasks/{taskId}/comments", new CreateCommentRequest("Sneaky."));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateComment_UnknownTask_ReturnsNotFound()
    {
        var admin = await RegisterAdminAsync();

        var response = await admin.Client.PostAsJsonAsync($"/api/tasks/{Guid.NewGuid()}/comments", new CreateCommentRequest("Hi."));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CreateComment_WithoutToken_ReturnsUnauthorized()
    {
        var admin = await RegisterAdminAsync();
        var taskId = await CreateTaskAsync(admin.Client);
        var anonymousClient = factory.CreateClient();

        var response = await anonymousClient.PostAsJsonAsync($"/api/tasks/{taskId}/comments", new CreateCommentRequest("Hi."));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetComments_ReturnsCommentsForTaskOrderedByCreation()
    {
        var admin = await RegisterAdminAsync();
        var taskId = await CreateTaskAsync(admin.Client);
        await admin.Client.PostAsJsonAsync($"/api/tasks/{taskId}/comments", new CreateCommentRequest("First."));
        await admin.Client.PostAsJsonAsync($"/api/tasks/{taskId}/comments", new CreateCommentRequest("Second."));

        var response = await admin.Client.GetAsync($"/api/tasks/{taskId}/comments");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var comments = await response.Content.ReadFromJsonAsync<List<CommentResponse>>(JsonOptions);
        Assert.NotNull(comments);
        Assert.Equal(2, comments!.Count);
        Assert.Equal("First.", comments[0].Body);
        Assert.Equal("Second.", comments[1].Body);
    }

    [Fact]
    public async Task GetComments_OnTaskFromAnotherOrg_ReturnsNotFound()
    {
        var admin = await RegisterAdminAsync();
        var taskId = await CreateTaskAsync(admin.Client);
        var otherAdmin = await RegisterAdminAsync();

        var response = await otherAdmin.Client.GetAsync($"/api/tasks/{taskId}/comments");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteComment_AsAuthor_RemovesComment()
    {
        var admin = await RegisterAdminAsync();
        var taskId = await CreateTaskAsync(admin.Client);
        var createResponse = await admin.Client.PostAsJsonAsync($"/api/tasks/{taskId}/comments", new CreateCommentRequest("Delete me."));
        var comment = (await createResponse.Content.ReadFromJsonAsync<CommentResponse>(JsonOptions))!;

        var response = await admin.Client.DeleteAsync($"/api/tasks/{taskId}/comments/{comment.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var getResponse = await admin.Client.GetAsync($"/api/tasks/{taskId}/comments");
        var comments = await getResponse.Content.ReadFromJsonAsync<List<CommentResponse>>(JsonOptions);
        Assert.DoesNotContain(comments!, c => c.Id == comment.Id);
    }

    [Fact]
    public async Task DeleteComment_AsAdminNonAuthor_RemovesComment()
    {
        var admin = await RegisterAdminAsync();
        var taskId = await CreateTaskAsync(admin.Client);
        var (memberClient, _) = await InviteAndAcceptMemberAsync(admin.Client, UniqueEmail());
        var createResponse = await memberClient.PostAsJsonAsync($"/api/tasks/{taskId}/comments", new CreateCommentRequest("Member comment."));
        var comment = (await createResponse.Content.ReadFromJsonAsync<CommentResponse>(JsonOptions))!;

        var response = await admin.Client.DeleteAsync($"/api/tasks/{taskId}/comments/{comment.Id}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteComment_AsNonAuthorNonAdmin_ReturnsForbidden()
    {
        var admin = await RegisterAdminAsync();
        var taskId = await CreateTaskAsync(admin.Client);
        var createResponse = await admin.Client.PostAsJsonAsync($"/api/tasks/{taskId}/comments", new CreateCommentRequest("Admin comment."));
        var comment = (await createResponse.Content.ReadFromJsonAsync<CommentResponse>(JsonOptions))!;
        var (memberClient, _) = await InviteAndAcceptMemberAsync(admin.Client, UniqueEmail());

        var response = await memberClient.DeleteAsync($"/api/tasks/{taskId}/comments/{comment.Id}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteComment_UnknownComment_ReturnsNotFound()
    {
        var admin = await RegisterAdminAsync();
        var taskId = await CreateTaskAsync(admin.Client);

        var response = await admin.Client.DeleteAsync($"/api/tasks/{taskId}/comments/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteComment_FromAnotherOrg_ReturnsNotFound()
    {
        var admin = await RegisterAdminAsync();
        var taskId = await CreateTaskAsync(admin.Client);
        var createResponse = await admin.Client.PostAsJsonAsync($"/api/tasks/{taskId}/comments", new CreateCommentRequest("Sensitive."));
        var comment = (await createResponse.Content.ReadFromJsonAsync<CommentResponse>(JsonOptions))!;
        var otherAdmin = await RegisterAdminAsync();

        var response = await otherAdmin.Client.DeleteAsync($"/api/tasks/{taskId}/comments/{comment.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
