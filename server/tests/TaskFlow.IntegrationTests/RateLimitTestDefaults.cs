using System.Runtime.CompilerServices;

namespace TaskFlow.IntegrationTests;

internal static class RateLimitTestDefaults
{
    [ModuleInitializer]
    public static void RelaxRegisterRateLimitForTests()
    {
        Environment.SetEnvironmentVariable("RateLimiting__Register__PermitLimit", "100000");
        Environment.SetEnvironmentVariable("RateLimiting__Register__WindowMinutes", "1");
    }
}
