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
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.IntegrationTests;

public class OrganizationEndpointsTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
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

    private async Task<InvitationResponse> InviteAsync(HttpClient adminClient, string email, UserRole role = UserRole.Member)
    {
        var response = await adminClient.PostAsJsonAsync("/api/organization/invitations", new CreateInvitationRequest(email, role));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<InvitationResponse>(JsonOptions))!;
    }

    private async Task<HttpClient> AcceptInvitationAsync(string token, string email)
    {
        var client = factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/accept-invitation", new AcceptInvitationRequest(
            token, "correct-horse-battery", "New", "Hire"));
        response.EnsureSuccessStatusCode();
        var body = (await response.Content.ReadFromJsonAsync<LoginResponse>(JsonOptions))!;
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.AccessToken);
        return client;
    }

    private async Task<string> ReadTokenFromDbAsync(Guid invitationId)
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var invitation = await db.Invitations.IgnoreQueryFilters().SingleAsync(i => i.Id == invitationId);
        return invitation.Token;
    }

    [Fact]
    public async Task GetOrganization_ReturnsOrgDetailsWithMemberCount()
    {
        var admin = await RegisterAdminAsync();

        var response = await admin.Client.GetAsync("/api/organization");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<OrganizationResponse>();
        Assert.NotNull(body);
        Assert.Equal(admin.Registered.OrganizationId, body!.Id);
        Assert.Equal(1, body.MemberCount);
    }

    [Fact]
    public async Task GetOrganization_WithoutToken_ReturnsUnauthorized()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/organization");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMembers_ReturnsOnlyMembersInCurrentTenant()
    {
        var orgA = await RegisterAdminAsync();
        var orgB = await RegisterAdminAsync();

        var response = await orgA.Client.GetAsync("/api/organization/members");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var members = await response.Content.ReadFromJsonAsync<List<MemberResponse>>(JsonOptions);
        Assert.NotNull(members);
        Assert.Single(members!);
        Assert.Equal(orgA.Registered.UserId, members![0].Id);
        Assert.DoesNotContain(members, m => m.Id == orgB.Registered.UserId);
    }

    [Fact]
    public async Task CreateInvitation_AsAdmin_ReturnsCreatedInvitation()
    {
        var admin = await RegisterAdminAsync();
        var email = UniqueEmail();

        var response = await admin.Client.PostAsJsonAsync("/api/organization/invitations", new CreateInvitationRequest(email, UserRole.Member));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<InvitationResponse>(JsonOptions);
        Assert.NotNull(body);
        Assert.Equal(email, body!.Email);
        Assert.Equal(UserRole.Member, body.Role);
        Assert.Equal(InvitationStatus.Pending, body.Status);
    }

    [Fact]
    public async Task CreateInvitation_AsNonAdmin_ReturnsForbidden()
    {
        var admin = await RegisterAdminAsync();
        var memberEmail = UniqueEmail();
        var invitation = await InviteAsync(admin.Client, memberEmail);
        var token = await ReadTokenFromDbAsync(invitation.Id);
        var memberClient = await AcceptInvitationAsync(token, memberEmail);

        var response = await memberClient.PostAsJsonAsync("/api/organization/invitations", new CreateInvitationRequest(UniqueEmail(), UserRole.Member));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateInvitation_WithInvalidEmail_ReturnsBadRequest()
    {
        var admin = await RegisterAdminAsync();

        var response = await admin.Client.PostAsJsonAsync("/api/organization/invitations", new CreateInvitationRequest("not-an-email", UserRole.Member));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateInvitation_ForExistingMemberEmail_ReturnsConflict()
    {
        var admin = await RegisterAdminAsync();
        var me = await (await admin.Client.GetAsync("/api/users/me")).Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var adminEmail = me.GetProperty("email").GetString()!;

        var response = await admin.Client.PostAsJsonAsync("/api/organization/invitations", new CreateInvitationRequest(adminEmail, UserRole.Member));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task CreateInvitation_WithAlreadyPendingInvite_ReturnsConflict()
    {
        var admin = await RegisterAdminAsync();
        var email = UniqueEmail();
        await InviteAsync(admin.Client, email);

        var response = await admin.Client.PostAsJsonAsync("/api/organization/invitations", new CreateInvitationRequest(email, UserRole.Member));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetInvitations_AsAdmin_ReturnsList()
    {
        var admin = await RegisterAdminAsync();
        var email = UniqueEmail();
        await InviteAsync(admin.Client, email);

        var response = await admin.Client.GetAsync("/api/organization/invitations");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var invitations = await response.Content.ReadFromJsonAsync<List<InvitationResponse>>(JsonOptions);
        Assert.NotNull(invitations);
        Assert.Contains(invitations!, i => i.Email == email);
    }

    [Fact]
    public async Task GetInvitations_AsNonAdmin_ReturnsForbidden()
    {
        var admin = await RegisterAdminAsync();
        var memberEmail = UniqueEmail();
        var invitation = await InviteAsync(admin.Client, memberEmail);
        var token = await ReadTokenFromDbAsync(invitation.Id);
        var memberClient = await AcceptInvitationAsync(token, memberEmail);

        var response = await memberClient.GetAsync("/api/organization/invitations");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task RevokeInvitation_AsAdmin_MarksInvitationRevoked()
    {
        var admin = await RegisterAdminAsync();
        var invitation = await InviteAsync(admin.Client, UniqueEmail());

        var deleteResponse = await admin.Client.DeleteAsync($"/api/organization/invitations/{invitation.Id}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var listResponse = await admin.Client.GetAsync("/api/organization/invitations");
        var invitations = await listResponse.Content.ReadFromJsonAsync<List<InvitationResponse>>(JsonOptions);
        Assert.Contains(invitations!, i => i.Id == invitation.Id && i.Status == InvitationStatus.Revoked);
    }

    [Fact]
    public async Task RevokeInvitation_UnknownId_ReturnsNotFound()
    {
        var admin = await RegisterAdminAsync();

        var response = await admin.Client.DeleteAsync($"/api/organization/invitations/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task RevokeInvitation_FromAnotherOrg_ReturnsNotFound()
    {
        var admin = await RegisterAdminAsync();
        var invitation = await InviteAsync(admin.Client, UniqueEmail());
        var otherAdmin = await RegisterAdminAsync();

        var response = await otherAdmin.Client.DeleteAsync($"/api/organization/invitations/{invitation.Id}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMemberRole_AsAdmin_ChangesRole()
    {
        var admin = await RegisterAdminAsync();
        var memberEmail = UniqueEmail();
        var invitation = await InviteAsync(admin.Client, memberEmail);
        var token = await ReadTokenFromDbAsync(invitation.Id);
        var memberClient = await AcceptInvitationAsync(token, memberEmail);
        var memberMe = await (await memberClient.GetAsync("/api/users/me")).Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var memberId = memberMe.GetProperty("id").GetGuid();

        var response = await admin.Client.PatchAsJsonAsync($"/api/organization/members/{memberId}/role", new UpdateMemberRoleRequest(UserRole.Admin));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<MemberRoleResponse>(JsonOptions);
        Assert.Equal(UserRole.Admin, body!.Role);
    }

    [Fact]
    public async Task UpdateMemberRole_AsNonAdmin_ReturnsForbidden()
    {
        var admin = await RegisterAdminAsync();
        var memberEmail = UniqueEmail();
        var invitation = await InviteAsync(admin.Client, memberEmail);
        var token = await ReadTokenFromDbAsync(invitation.Id);
        var memberClient = await AcceptInvitationAsync(token, memberEmail);

        var response = await memberClient.PatchAsJsonAsync($"/api/organization/members/{admin.Registered.UserId}/role", new UpdateMemberRoleRequest(UserRole.Admin));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMemberRole_UnknownUser_ReturnsNotFound()
    {
        var admin = await RegisterAdminAsync();

        var response = await admin.Client.PatchAsJsonAsync($"/api/organization/members/{Guid.NewGuid()}/role", new UpdateMemberRoleRequest(UserRole.Admin));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateMemberRole_MemberFromAnotherOrg_ReturnsNotFound()
    {
        var admin = await RegisterAdminAsync();
        var otherAdmin = await RegisterAdminAsync();

        var response = await admin.Client.PatchAsJsonAsync($"/api/organization/members/{otherAdmin.Registered.UserId}/role", new UpdateMemberRoleRequest(UserRole.Admin));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateMember_AsAdmin_SetsStatusDeactivatedAndRevokesSessions()
    {
        var admin = await RegisterAdminAsync();
        var memberEmail = UniqueEmail();
        var invitation = await InviteAsync(admin.Client, memberEmail);
        var token = await ReadTokenFromDbAsync(invitation.Id);
        var memberClient = await AcceptInvitationAsync(token, memberEmail);
        var memberMe = await (await memberClient.GetAsync("/api/users/me")).Content.ReadFromJsonAsync<JsonElement>(JsonOptions);
        var memberId = memberMe.GetProperty("id").GetGuid();

        var deleteResponse = await admin.Client.DeleteAsync($"/api/organization/members/{memberId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var membersResponse = await admin.Client.GetAsync("/api/organization/members");
        var members = await membersResponse.Content.ReadFromJsonAsync<List<MemberResponse>>(JsonOptions);
        Assert.Contains(members!, m => m.Id == memberId && m.Status == UserStatus.Deactivated);

        var loginResponse = await factory.CreateClient().PostAsJsonAsync("/api/auth/login", new LoginRequest(memberEmail, "correct-horse-battery"));
        Assert.Equal(HttpStatusCode.Unauthorized, loginResponse.StatusCode);
    }

    [Fact]
    public async Task DeactivateMember_AsNonAdmin_ReturnsForbidden()
    {
        var admin = await RegisterAdminAsync();
        var memberEmail = UniqueEmail();
        var invitation = await InviteAsync(admin.Client, memberEmail);
        var token = await ReadTokenFromDbAsync(invitation.Id);
        var memberClient = await AcceptInvitationAsync(token, memberEmail);

        var response = await memberClient.DeleteAsync($"/api/organization/members/{admin.Registered.UserId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateMember_UnknownUser_ReturnsNotFound()
    {
        var admin = await RegisterAdminAsync();

        var response = await admin.Client.DeleteAsync($"/api/organization/members/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeactivateMember_MemberFromAnotherOrg_ReturnsNotFound()
    {
        var admin = await RegisterAdminAsync();
        var otherAdmin = await RegisterAdminAsync();

        var response = await admin.Client.DeleteAsync($"/api/organization/members/{otherAdmin.Registered.UserId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
