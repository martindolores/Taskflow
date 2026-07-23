using TaskFlow.Application.Activity;

namespace TaskFlow.Api.Endpoints;

public static class ActivityEndpoints
{
    public static IEndpointRouteBuilder MapActivityEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/activity").RequireAuthorization();

        group.MapGet("/", async (IActivityService activityService, CancellationToken cancellationToken, int limit = 20) =>
        {
            var response = await activityService.GetActivityAsync(limit, cancellationToken);
            return Results.Ok(response);
        });

        return app;
    }
}
