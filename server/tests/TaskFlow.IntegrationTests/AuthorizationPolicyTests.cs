using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace TaskFlow.IntegrationTests;

public class AuthorizationPolicyTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact]
    public async Task AdminOnlyPolicy_RequiresAdminRoleClaim()
    {
        var provider = factory.Services.GetRequiredService<IAuthorizationPolicyProvider>();

        var policy = await provider.GetPolicyAsync("AdminOnly");

        Assert.NotNull(policy);
        var requirement = Assert.IsType<RolesAuthorizationRequirement>(Assert.Single(policy!.Requirements));
        Assert.Contains("Admin", requirement.AllowedRoles);
    }
}
