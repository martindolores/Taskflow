using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Auth;
using TaskFlow.Application.Auth.Dtos;
using TaskFlow.Application.Auth.Exceptions;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Auth;

public sealed class AuthService(
    AppDbContext db,
    IPasswordHasher passwordHasher,
    IJwtTokenService jwtTokenService) : IAuthService
{
    public async Task<RegisterResponse> RegisterOrganizationAsync(RegisterRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();

        if (await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email, cancellationToken))
        {
            throw new EmailAlreadyInUseException(email);
        }

        var organization = new Organization
        {
            Id = Guid.NewGuid(),
            Name = request.OrganizationName,
            Slug = await GenerateUniqueSlugAsync(request.OrganizationName, cancellationToken),
        };

        var user = new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = organization.Id,
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = UserRole.Admin,
            Status = UserStatus.Active,
        };

        db.Organizations.Add(organization);
        db.Users.Add(user);

        var (accessToken, refreshToken) = IssueTokens(user);

        await db.SaveChangesAsync(cancellationToken);

        return new RegisterResponse(user.Id, organization.Id, accessToken, refreshToken);
    }

    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.Users.IgnoreQueryFilters().SingleOrDefaultAsync(u => u.Email == email, cancellationToken);

        if (user is null || user.Status == UserStatus.Deactivated || !passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            throw new InvalidCredentialsException();
        }

        var (accessToken, refreshToken) = IssueTokens(user);

        await db.SaveChangesAsync(cancellationToken);

        return new LoginResponse(
            accessToken,
            refreshToken,
            new AuthenticatedUser(user.Id, user.Email, user.FirstName, user.LastName, user.Role, user.OrganizationId));
    }

    public async Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken)
    {
        var existing = await FindActiveRefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (existing is null)
        {
            throw new InvalidRefreshTokenException();
        }

        existing.RevokedAt = DateTime.UtcNow;

        var (accessToken, refreshToken) = IssueTokens(existing.User!);

        await db.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResponse(accessToken, refreshToken);
    }

    public async Task<LoginResponse> AcceptInvitationAsync(AcceptInvitationRequest request, CancellationToken cancellationToken)
    {
        var invitation = await db.Invitations
            .IgnoreQueryFilters()
            .SingleOrDefaultAsync(i => i.Token == request.Token, cancellationToken);

        var isValid = invitation is { Status: InvitationStatus.Pending } && invitation.ExpiresAt > DateTime.UtcNow;

        if (!isValid)
        {
            throw new InvalidInvitationException();
        }

        var email = invitation!.Email;

        if (await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Email == email, cancellationToken))
        {
            throw new EmailAlreadyInUseException(email);
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            OrganizationId = invitation.OrganizationId,
            Email = email,
            PasswordHash = passwordHasher.Hash(request.Password),
            FirstName = request.FirstName,
            LastName = request.LastName,
            Role = invitation.Role,
            Status = UserStatus.Active,
        };

        invitation.Status = InvitationStatus.Accepted;

        db.Users.Add(user);

        var (accessToken, refreshToken) = IssueTokens(user);

        await db.SaveChangesAsync(cancellationToken);

        return new LoginResponse(
            accessToken,
            refreshToken,
            new AuthenticatedUser(user.Id, user.Email, user.FirstName, user.LastName, user.Role, user.OrganizationId));
    }

    public async Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken)
    {
        var existing = await FindActiveRefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (existing is null)
        {
            return;
        }

        existing.RevokedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<RefreshToken?> FindActiveRefreshTokenAsync(string rawToken, CancellationToken cancellationToken)
    {
        var tokenHash = jwtTokenService.HashRefreshToken(rawToken);

        var refreshToken = await db.RefreshTokens
            .IgnoreQueryFilters()
            .Include(r => r.User)
            .SingleOrDefaultAsync(r => r.TokenHash == tokenHash, cancellationToken);

        var isActive = refreshToken is { RevokedAt: null } && refreshToken.ExpiresAt > DateTime.UtcNow;

        return isActive ? refreshToken : null;
    }

    private (string AccessToken, string RefreshToken) IssueTokens(User user)
    {
        var accessToken = jwtTokenService.CreateAccessToken(user);
        var refreshToken = jwtTokenService.CreateRefreshToken();

        db.RefreshTokens.Add(new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = jwtTokenService.HashRefreshToken(refreshToken.Token),
            ExpiresAt = refreshToken.ExpiresAt,
        });

        return (accessToken, refreshToken.Token);
    }

    private async Task<string> GenerateUniqueSlugAsync(string name, CancellationToken cancellationToken)
    {
        var baseSlug = SlugGenerator.Generate(name);
        var slug = baseSlug;
        var suffix = 1;

        while (await db.Organizations.AnyAsync(o => o.Slug == slug, cancellationToken))
        {
            slug = $"{baseSlug}-{++suffix}";
        }

        return slug;
    }
}
