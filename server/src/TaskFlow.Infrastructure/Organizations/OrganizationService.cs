using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Auth.Exceptions;
using TaskFlow.Application.Common;
using TaskFlow.Application.Organizations;
using TaskFlow.Application.Organizations.Dtos;
using TaskFlow.Application.Organizations.Exceptions;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Organizations;

public sealed class OrganizationService(AppDbContext db, ICurrentUserService currentUserService) : IOrganizationService
{
    public async Task<OrganizationResponse> GetOrganizationAsync(CancellationToken cancellationToken)
    {
        var organizationId = currentUserService.OrganizationId!.Value;

        var organization = await db.Organizations.SingleAsync(o => o.Id == organizationId, cancellationToken);
        var memberCount = await db.Users.CountAsync(u => u.Status != UserStatus.Deactivated, cancellationToken);

        return new OrganizationResponse(organization.Id, organization.Name, organization.Slug, memberCount);
    }

    public async Task<IReadOnlyList<MemberResponse>> GetMembersAsync(CancellationToken cancellationToken) =>
        await db.Users
            .OrderBy(u => u.FirstName)
            .ThenBy(u => u.LastName)
            .Select(u => new MemberResponse(u.Id, u.Email, u.FirstName, u.LastName, u.Role, u.Status))
            .ToListAsync(cancellationToken);

    public async Task<InvitationResponse> CreateInvitationAsync(CreateInvitationRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email, cancellationToken))
        {
            throw new EmailAlreadyInUseException(email);
        }

        if (await db.Invitations.AnyAsync(i => i.Email == email && i.Status == InvitationStatus.Pending, cancellationToken))
        {
            throw new InvitationAlreadyPendingException(email);
        }

        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            OrganizationId = currentUserService.OrganizationId!.Value,
            Email = email,
            Role = request.Role,
            Token = RandomNumberGenerator.GetHexString(64),
            InvitedById = currentUserService.UserId!.Value,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };

        db.Invitations.Add(invitation);
        db.ActivityLog.Add(new ActivityLogEntry
        {
            Id = Guid.NewGuid(),
            OrganizationId = invitation.OrganizationId,
            ActorId = currentUserService.UserId!.Value,
            TaskId = null,
            Type = ActivityType.MemberInvited,
            Summary = $"invited {email}",
        });

        await db.SaveChangesAsync(cancellationToken);

        return ToInvitationResponse(invitation);
    }

    public async Task<IReadOnlyList<InvitationResponse>> GetInvitationsAsync(CancellationToken cancellationToken) =>
        await db.Invitations
            .OrderByDescending(i => i.CreatedAt)
            .Select(i => new InvitationResponse(i.Id, i.Email, i.Role, i.Status, i.ExpiresAt, i.Token))
            .ToListAsync(cancellationToken);

    public async Task RevokeInvitationAsync(Guid invitationId, CancellationToken cancellationToken)
    {
        var invitation = await db.Invitations.SingleOrDefaultAsync(i => i.Id == invitationId, cancellationToken)
            ?? throw new InvitationNotFoundException(invitationId);

        if (invitation.Status != InvitationStatus.Pending)
        {
            return;
        }

        invitation.Status = InvitationStatus.Revoked;
        await db.SaveChangesAsync(cancellationToken);
    }

    public async Task<MemberRoleResponse> UpdateMemberRoleAsync(Guid userId, UpdateMemberRoleRequest request, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new MemberNotFoundException(userId);

        user.Role = request.Role;
        await db.SaveChangesAsync(cancellationToken);

        return new MemberRoleResponse(user.Id, user.Role);
    }

    public async Task DeactivateMemberAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await db.Users.SingleOrDefaultAsync(u => u.Id == userId, cancellationToken)
            ?? throw new MemberNotFoundException(userId);

        user.Status = UserStatus.Deactivated;

        var activeTokens = await db.RefreshTokens
            .Where(r => r.UserId == userId && r.RevokedAt == null)
            .ToListAsync(cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAt = DateTime.UtcNow;
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private static InvitationResponse ToInvitationResponse(Invitation invitation) =>
        new(invitation.Id, invitation.Email, invitation.Role, invitation.Status, invitation.ExpiresAt, invitation.Token);
}
