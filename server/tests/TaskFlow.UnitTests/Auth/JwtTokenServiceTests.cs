using System.IdentityModel.Tokens.Jwt;
using Microsoft.Extensions.Options;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Auth;

namespace TaskFlow.UnitTests.Auth;

public class JwtTokenServiceTests
{
    private readonly JwtTokenService _service = new(Options.Create(new JwtOptions
    {
        Secret = "unit-test-signing-secret-at-least-32-bytes-long",
        Issuer = "TaskFlow.Tests",
        Audience = "TaskFlow.Tests",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 14,
    }));

    private static User CreateUser() => new()
    {
        Id = Guid.NewGuid(),
        OrganizationId = Guid.NewGuid(),
        Email = "user@acme.com",
        PasswordHash = "irrelevant",
        FirstName = "Ada",
        LastName = "Lovelace",
        Role = UserRole.Admin,
        Status = UserStatus.Active,
    };

    [Fact]
    public void CreateAccessToken_IncludesExpectedClaims()
    {
        var user = CreateUser();

        var token = _service.CreateAccessToken(user);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Claims.Single(c => c.Type == "sub").Value);
        Assert.Equal(user.OrganizationId.ToString(), jwt.Claims.Single(c => c.Type == "org").Value);
        Assert.Equal("Admin", jwt.Claims.Single(c => c.Type == "role").Value);
    }

    [Fact]
    public void CreateRefreshToken_ProducesUniqueTokensWithFutureExpiry()
    {
        var first = _service.CreateRefreshToken();
        var second = _service.CreateRefreshToken();

        Assert.NotEqual(first.Token, second.Token);
        Assert.True(first.ExpiresAt > DateTime.UtcNow);
    }

    [Fact]
    public void HashRefreshToken_IsDeterministic()
    {
        var raw = "some-refresh-token-value";

        Assert.Equal(_service.HashRefreshToken(raw), _service.HashRefreshToken(raw));
    }

    [Fact]
    public void HashRefreshToken_DiffersForDifferentInput()
    {
        Assert.NotEqual(_service.HashRefreshToken("a"), _service.HashRefreshToken("b"));
    }
}
