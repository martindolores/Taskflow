using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using TaskFlow.Application.Common;
using TaskFlow.Application.Organizations.Dtos;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Email;
using TaskFlow.Infrastructure.Organizations;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.UnitTests.Organizations;

public class OrganizationServiceTests
{
    private readonly Guid _organizationId = Guid.NewGuid();
    private readonly Guid _adminId = Guid.NewGuid();

    private AppDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options, new FakeCurrentTenantService(_organizationId));
    }

    private async Task<AppDbContext> SeedDbContextAsync()
    {
        var db = CreateDbContext();

        db.Organizations.Add(new Organization
        {
            Id = _organizationId,
            Name = "Acme Inc",
            Slug = "acme-inc",
        });
        db.Users.Add(new User
        {
            Id = _adminId,
            OrganizationId = _organizationId,
            Email = "ada@acme.com",
            PasswordHash = "hash",
            FirstName = "Ada",
            LastName = "Lovelace",
            Role = UserRole.Admin,
            Status = UserStatus.Active,
        });

        await db.SaveChangesAsync();
        return db;
    }

    private OrganizationService CreateService(AppDbContext db, SpyEmailService emailService, string frontendBaseUrl = "https://app.taskflow.example") =>
        new(
            db,
            new FakeCurrentUserService(_adminId, _organizationId, UserRole.Admin),
            emailService,
            Options.Create(new EmailOptions { FrontendBaseUrl = frontendBaseUrl }));

    [Fact]
    public async Task CreateInvitationAsync_OnSuccess_SendsEmailWithOrgRoleAndAcceptLink_AndReturnsEmailSentTrue()
    {
        var db = await SeedDbContextAsync();
        var emailService = new SpyEmailService(result: true);
        var service = CreateService(db, emailService);

        var result = await service.CreateInvitationAsync(new CreateInvitationRequest("newhire@acme.com", UserRole.Member), CancellationToken.None);

        Assert.True(result.EmailSent);
        Assert.NotNull(emailService.LastCall);
        Assert.Equal("newhire@acme.com", emailService.LastCall!.ToEmail);
        Assert.Equal("Acme Inc", emailService.LastCall.OrganizationName);
        Assert.Equal("Ada Lovelace", emailService.LastCall.InviterName);
        Assert.Equal(UserRole.Member, emailService.LastCall.Role);
        Assert.Equal($"https://app.taskflow.example/accept-invitation?token={result.Token}", emailService.LastCall.AcceptUrl);
        Assert.Equal(result.ExpiresAt, emailService.LastCall.ExpiresAt);
    }

    [Fact]
    public async Task CreateInvitationAsync_WhenEmailServiceFails_StillCreatesInvitation_AndReturnsEmailSentFalse()
    {
        var db = await SeedDbContextAsync();
        var emailService = new SpyEmailService(result: false);
        var service = CreateService(db, emailService);

        var result = await service.CreateInvitationAsync(new CreateInvitationRequest("newhire@acme.com", UserRole.Member), CancellationToken.None);

        Assert.False(result.EmailSent);
        Assert.NotNull(emailService.LastCall);

        var persisted = await db.Invitations.SingleAsync(i => i.Id == result.Id);
        Assert.Equal(InvitationStatus.Pending, persisted.Status);
    }

    private sealed class FakeCurrentTenantService(Guid organizationId) : ICurrentTenantService
    {
        public Guid? OrganizationId { get; } = organizationId;
    }

    private sealed class FakeCurrentUserService(Guid userId, Guid organizationId, UserRole role) : ICurrentUserService
    {
        public bool IsAuthenticated => true;

        public Guid? UserId { get; } = userId;

        public Guid? OrganizationId { get; } = organizationId;

        public UserRole? Role { get; } = role;
    }

    private sealed record EmailCall(string ToEmail, string OrganizationName, string InviterName, UserRole Role, string AcceptUrl, DateTime ExpiresAt);

    private sealed class SpyEmailService(bool result) : IEmailService
    {
        public EmailCall? LastCall { get; private set; }

        public Task<bool> SendInvitationEmailAsync(
            string toEmail,
            string organizationName,
            string inviterName,
            UserRole role,
            string acceptUrl,
            DateTime expiresAt,
            CancellationToken cancellationToken)
        {
            LastCall = new EmailCall(toEmail, organizationName, inviterName, role, acceptUrl, expiresAt);
            return Task.FromResult(result);
        }
    }
}
