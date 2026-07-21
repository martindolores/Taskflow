using TaskFlow.Application.Common;
using TaskFlow.Application.Tasks.Dtos;

namespace TaskFlow.Application.Tasks;

public interface ITaskService
{
    Task<PagedResult<TaskListItemResponse>> GetTasksAsync(TaskListQuery query, CancellationToken cancellationToken);

    Task<TaskResponse> GetTaskAsync(Guid taskId, CancellationToken cancellationToken);

    Task<CreateTaskResponse> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken);

    Task<TaskResponse> UpdateTaskAsync(Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken);

    Task<TaskStatusResponse> UpdateTaskStatusAsync(Guid taskId, UpdateTaskStatusRequest request, CancellationToken cancellationToken);

    Task DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken);
}
