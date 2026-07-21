using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Auth.Exceptions;

namespace TaskFlow.Api.Middleware;

public sealed class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (status, title) = MapException(exception);

        if (status == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled exception processing {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }
        else
        {
            logger.LogWarning(
                exception,
                "Request failed processing {Method} {Path}",
                httpContext.Request.Method,
                httpContext.Request.Path);
        }

        var problemDetails = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.6.1",
            Instance = httpContext.Request.Path,
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        await httpContext.Response.WriteAsJsonAsync(problemDetails, cancellationToken);

        return true;
    }

    private static (int Status, string Title) MapException(Exception exception) => exception switch
    {
        EmailAlreadyInUseException => (StatusCodes.Status409Conflict, exception.Message),
        InvalidCredentialsException => (StatusCodes.Status401Unauthorized, exception.Message),
        InvalidRefreshTokenException => (StatusCodes.Status401Unauthorized, exception.Message),
        _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred."),
    };
}
