using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Activity.Dtos;

public sealed record ActivityResponse(
    Guid Id,
    Guid ActorId,
    string ActorName,
    Guid? TaskId,
    ActivityType Type,
    string Summary,
    DateTime CreatedAt);
