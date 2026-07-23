using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Tasks.Dtos;

public sealed record CreateTaskResponse(
    Guid Id,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    Guid? AssigneeId,
    DateOnly? DueDate,
    Guid? ProjectId,
    DateTime CreatedAt);
