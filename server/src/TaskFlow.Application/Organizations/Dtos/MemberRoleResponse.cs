using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Organizations.Dtos;

public sealed record MemberRoleResponse(Guid Id, UserRole Role);
