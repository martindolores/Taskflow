using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Organizations.Dtos;

public sealed record UpdateMemberRoleRequest(UserRole Role);
