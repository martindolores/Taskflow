using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Organizations.Dtos;

public sealed record CreateInvitationRequest(string Email, UserRole Role);
