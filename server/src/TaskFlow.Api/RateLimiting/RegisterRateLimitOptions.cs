namespace TaskFlow.Api.RateLimiting;

public sealed class RegisterRateLimitOptions
{
    public int PermitLimit { get; init; } = 5;
    public int WindowMinutes { get; init; } = 10;
}
