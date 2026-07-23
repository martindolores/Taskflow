using TaskFlow.Api.Filters;
using TaskFlow.Application.Projects;
using TaskFlow.Application.Projects.Dtos;

namespace TaskFlow.Api.Endpoints;

public static class ProjectEndpoints
{
    public static IEndpointRouteBuilder MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/projects").RequireAuthorization();

        group.MapGet("/", async (IProjectService projectService, CancellationToken cancellationToken) =>
        {
            var response = await projectService.GetProjectsAsync(cancellationToken);
            return Results.Ok(response);
        });

        group.MapPost("/", async (CreateProjectRequest request, IProjectService projectService, CancellationToken cancellationToken) =>
            {
                var response = await projectService.CreateProjectAsync(request, cancellationToken);
                return Results.Created($"/api/projects/{response.Id}", response);
            })
            .RequireAuthorization("AdminOnly")
            .AddEndpointFilter<ValidationFilter<CreateProjectRequest>>();

        group.MapDelete("/{id:guid}", async (Guid id, IProjectService projectService, CancellationToken cancellationToken) =>
            {
                await projectService.DeleteProjectAsync(id, cancellationToken);
                return Results.NoContent();
            })
            .RequireAuthorization("AdminOnly");

        return app;
    }
}
