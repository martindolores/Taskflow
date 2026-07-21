namespace TaskFlow.Application.Auth.Dtos;

public sealed record LoginResponse(string AccessToken, string RefreshToken, AuthenticatedUser User);
