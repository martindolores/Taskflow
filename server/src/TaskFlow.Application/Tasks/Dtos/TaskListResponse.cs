namespace TaskFlow.Application.Tasks.Dtos;

public sealed record TaskListResponse(
    IReadOnlyList<TaskListItemResponse> Items,
    int Total,
    int Page,
    int PageSize);
