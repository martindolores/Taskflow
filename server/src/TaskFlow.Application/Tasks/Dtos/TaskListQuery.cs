using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Tasks.Dtos;

public sealed record TaskListQuery(
    TaskItemStatus? Status,
    TaskPriority? Priority,
    Guid? AssigneeId,
    int Page,
    int PageSize);
