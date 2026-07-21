using TaskFlow.Application.Users;

namespace TaskFlow.Api.Endpoints;

public static class UserEndpoints
{
    public static IEndpointRouteBuilder MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").RequireAuthorization();

        group.MapGet("/me", async (IUserService userService, CancellationToken cancellationToken) =>
        {
            var response = await userService.GetCurrentUserAsync(cancellationToken);
            return Results.Ok(response);
        });

        return app;
    }
}
