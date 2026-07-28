namespace TaskFlow.Application.Auth.Dtos;

public sealed record RegisterResponse(
    Guid UserId,
    Guid OrganizationId,
    bool EmailConfirmationRequired,
    string? AccessToken,
    string? RefreshToken);
