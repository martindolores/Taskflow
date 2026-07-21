namespace TaskFlow.Application.Auth.Dtos;

public sealed record RegisterResponse(
    Guid UserId,
    Guid OrganizationId,
    string AccessToken,
    string RefreshToken);
