namespace TaskFlow.Application.Auth.Dtos;

public sealed record RegisterRequest(
    string OrganizationName,
    string Email,
    string Password,
    string FirstName,
    string LastName);
