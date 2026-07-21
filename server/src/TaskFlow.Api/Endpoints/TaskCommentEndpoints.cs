using TaskFlow.Api.Filters;
using TaskFlow.Application.TaskComments;
using TaskFlow.Application.TaskComments.Dtos;

namespace TaskFlow.Api.Endpoints;

public static class TaskCommentEndpoints
{
    public static IEndpointRouteBuilder MapTaskCommentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks/{taskId:guid}/comments").RequireAuthorization();

        group.MapGet("/", async (Guid taskId, ITaskCommentService commentService, CancellationToken cancellationToken) =>
        {
            var response = await commentService.GetCommentsAsync(taskId, cancellationToken);
            return Results.Ok(response);
        });

        group.MapPost("/", async (Guid taskId, CreateCommentRequest request, ITaskCommentService commentService, CancellationToken cancellationToken) =>
            {
                var response = await commentService.CreateCommentAsync(taskId, request, cancellationToken);
                return Results.Created($"/api/tasks/{taskId}/comments/{response.Id}", response);
            })
            .AddEndpointFilter<ValidationFilter<CreateCommentRequest>>();

        group.MapDelete("/{commentId:guid}", async (Guid taskId, Guid commentId, ITaskCommentService commentService, CancellationToken cancellationToken) =>
        {
            await commentService.DeleteCommentAsync(taskId, commentId, cancellationToken);
            return Results.NoContent();
        });

        return app;
    }
}
