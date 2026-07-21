using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Tasks.Dtos;

public sealed record TaskStatusResponse(Guid Id, TaskItemStatus Status);
