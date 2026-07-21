using TaskFlow.Api.Filters;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Tasks.Dtos;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Api.Endpoints;

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks").RequireAuthorization();

        group.MapGet("/", async (
            TaskItemStatus? status,
            TaskPriority? priority,
            Guid? assigneeId,
            ITaskService taskService,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = 20) =>
        {
            var query = new TaskListQuery(status, priority, assigneeId, page, pageSize);
            var response = await taskService.GetTasksAsync(query, cancellationToken);
            return Results.Ok(response);
        });

        group.MapGet("/{id:guid}", async (Guid id, ITaskService taskService, CancellationToken cancellationToken) =>
        {
            var response = await taskService.GetTaskAsync(id, cancellationToken);
            return Results.Ok(response);
        });

        group.MapPost("/", async (CreateTaskRequest request, ITaskService taskService, CancellationToken cancellationToken) =>
            {
                var response = await taskService.CreateTaskAsync(request, cancellationToken);
                return Results.Created($"/api/tasks/{response.Id}", response);
            })
            .AddEndpointFilter<ValidationFilter<CreateTaskRequest>>();

        group.MapPut("/{id:guid}", async (Guid id, UpdateTaskRequest request, ITaskService taskService, CancellationToken cancellationToken) =>
            {
                var response = await taskService.UpdateTaskAsync(id, request, cancellationToken);
                return Results.Ok(response);
            })
            .AddEndpointFilter<ValidationFilter<UpdateTaskRequest>>();

        group.MapPatch("/{id:guid}/status", async (Guid id, UpdateTaskStatusRequest request, ITaskService taskService, CancellationToken cancellationToken) =>
            {
                var response = await taskService.UpdateTaskStatusAsync(id, request, cancellationToken);
                return Results.Ok(response);
            })
            .AddEndpointFilter<ValidationFilter<UpdateTaskStatusRequest>>();

        group.MapDelete("/{id:guid}", async (Guid id, ITaskService taskService, CancellationToken cancellationToken) =>
        {
            await taskService.DeleteTaskAsync(id, cancellationToken);
            return Results.NoContent();
        });

        return app;
    }
}
