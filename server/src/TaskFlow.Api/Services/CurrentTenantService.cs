using TaskFlow.Application.Common;

namespace TaskFlow.Api.Services;

public sealed class CurrentTenantService(ICurrentUserService currentUserService) : ICurrentTenantService
{
    public Guid? OrganizationId => currentUserService.OrganizationId;
}
