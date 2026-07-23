using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Tasks.Dtos;

public sealed record CreateTaskRequest(
    string Title,
    string? Description,
    TaskPriority Priority,
    Guid? AssigneeId,
    DateOnly? DueDate,
    Guid? ProjectId = null);
