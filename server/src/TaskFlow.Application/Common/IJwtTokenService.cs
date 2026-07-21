using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Common;

public interface IJwtTokenService
{
    string CreateAccessToken(User user);

    RefreshTokenResult CreateRefreshToken();

    string HashRefreshToken(string rawToken);
}

public sealed record RefreshTokenResult(string Token, DateTime ExpiresAt);
