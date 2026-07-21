using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Organizations.Dtos;

public sealed record MemberResponse(
    Guid Id,
    string Email,
    string FirstName,
    string LastName,
    UserRole Role,
    UserStatus Status);
