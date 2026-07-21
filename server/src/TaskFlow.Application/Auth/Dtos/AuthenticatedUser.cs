using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Auth.Dtos;

public sealed record AuthenticatedUser(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    Guid OrganizationId);
