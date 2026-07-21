using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Organizations.Dtos;

public sealed record InvitationResponse(Guid Id, string Email, UserRole Role, InvitationStatus Status, DateTime ExpiresAt);
