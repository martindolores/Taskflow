using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using TaskFlow.Api.Services;
using TaskFlow.Domain.Enums;

namespace TaskFlow.UnitTests.Common;

public class CurrentUserServiceTests
{
    private static CurrentUserService CreateService(ClaimsPrincipal? user)
    {
        var accessor = new HttpContextAccessor();
        if (user is not null)
        {
            accessor.HttpContext = new DefaultHttpContext { User = user };
        }

        return new CurrentUserService(accessor);
    }

    private static ClaimsPrincipal AuthenticatedUser(params Claim[] claims) =>
        new(new ClaimsIdentity(claims, "TestAuth"));

    [Fact]
    public void IsAuthenticated_WithAuthenticatedIdentity_ReturnsTrue()
    {
        var service = CreateService(AuthenticatedUser(new Claim("sub", Guid.NewGuid().ToString())));

        Assert.True(service.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_WithNoHttpContext_ReturnsFalse()
    {
        var service = CreateService(null);

        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public void IsAuthenticated_WithUnauthenticatedPrincipal_ReturnsFalse()
    {
        var service = CreateService(new ClaimsPrincipal(new ClaimsIdentity()));

        Assert.False(service.IsAuthenticated);
    }

    [Fact]
    public void UserId_WithValidSubClaim_ReturnsParsedGuid()
    {
        var userId = Guid.NewGuid();
        var service = CreateService(AuthenticatedUser(new Claim("sub", userId.ToString())));

        Assert.Equal(userId, service.UserId);
    }

    [Fact]
    public void UserId_WithMissingSubClaim_ReturnsNull()
    {
        var service = CreateService(AuthenticatedUser());

        Assert.Null(service.UserId);
    }

    [Fact]
    public void UserId_WithMalformedSubClaim_ReturnsNull()
    {
        var service = CreateService(AuthenticatedUser(new Claim("sub", "not-a-guid")));

        Assert.Null(service.UserId);
    }

    [Fact]
    public void OrganizationId_WithValidOrgClaim_ReturnsParsedGuid()
    {
        var organizationId = Guid.NewGuid();
        var service = CreateService(AuthenticatedUser(new Claim("org", organizationId.ToString())));

        Assert.Equal(organizationId, service.OrganizationId);
    }

    [Fact]
    public void OrganizationId_WithMissingOrgClaim_ReturnsNull()
    {
        var service = CreateService(AuthenticatedUser());

        Assert.Null(service.OrganizationId);
    }

    [Fact]
    public void Role_WithValidRoleClaim_ReturnsParsedRole()
    {
        var service = CreateService(AuthenticatedUser(new Claim("role", "Admin")));

        Assert.Equal(UserRole.Admin, service.Role);
    }

    [Fact]
    public void Role_WithMissingRoleClaim_ReturnsNull()
    {
        var service = CreateService(AuthenticatedUser());

        Assert.Null(service.Role);
    }

    [Fact]
    public void Role_WithUnrecognizedRoleClaim_ReturnsNull()
    {
        var service = CreateService(AuthenticatedUser(new Claim("role", "SuperAdmin")));

        Assert.Null(service.Role);
    }
}
