using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Tasks.Dtos;

public sealed record TaskListItemResponse(
    Guid Id,
    string Title,
    TaskItemStatus Status,
    TaskPriority Priority,
    Guid? AssigneeId,
    string? AssigneeName,
    DateOnly? DueDate,
    DateTime CreatedAt);
