using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Users.Dtos;

public sealed record CurrentUserResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    Guid OrganizationId,
    string OrganizationName);
