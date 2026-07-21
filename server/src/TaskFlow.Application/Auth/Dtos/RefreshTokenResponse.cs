namespace TaskFlow.Application.Auth.Dtos;

public sealed record RefreshTokenResponse(string AccessToken, string RefreshToken);
