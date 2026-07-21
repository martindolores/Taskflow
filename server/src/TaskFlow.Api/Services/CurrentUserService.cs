using System.Security.Claims;
using TaskFlow.Application.Common;
using TaskFlow.Domain.Enums;

namespace TaskFlow.Api.Services;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId => Guid.TryParse(Principal?.FindFirstValue("sub"), out var id) ? id : null;

    public Guid? OrganizationId => Guid.TryParse(Principal?.FindFirstValue("org"), out var id) ? id : null;

    public UserRole? Role => Enum.TryParse<UserRole>(Principal?.FindFirstValue("role"), out var role) ? role : null;
}
