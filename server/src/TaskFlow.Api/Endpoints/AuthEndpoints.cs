using TaskFlow.Api.Filters;
using TaskFlow.Application.Auth;
using TaskFlow.Application.Auth.Dtos;

namespace TaskFlow.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth");

        group.MapPost("/register", async (RegisterRequest request, IAuthService authService, CancellationToken cancellationToken) =>
            {
                var response = await authService.RegisterOrganizationAsync(request, cancellationToken);
                return Results.Created($"/api/users/{response.UserId}", response);
            })
            .AddEndpointFilter<ValidationFilter<RegisterRequest>>();

        group.MapPost("/login", async (LoginRequest request, IAuthService authService, CancellationToken cancellationToken) =>
            {
                var response = await authService.LoginAsync(request, cancellationToken);
                return Results.Ok(response);
            })
            .AddEndpointFilter<ValidationFilter<LoginRequest>>();

        group.MapPost("/refresh", async (RefreshTokenRequest request, IAuthService authService, CancellationToken cancellationToken) =>
            {
                var response = await authService.RefreshTokenAsync(request, cancellationToken);
                return Results.Ok(response);
            })
            .AddEndpointFilter<ValidationFilter<RefreshTokenRequest>>();

        group.MapPost("/logout", async (LogoutRequest request, IAuthService authService, CancellationToken cancellationToken) =>
            {
                await authService.LogoutAsync(request, cancellationToken);
                return Results.NoContent();
            })
            .AddEndpointFilter<ValidationFilter<LogoutRequest>>();

        return app;
    }
}
