using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Tasks.Dtos;

public sealed record UpdateTaskStatusRequest(TaskItemStatus Status);
