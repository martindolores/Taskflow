namespace TaskFlow.Application.Common;

public interface ICurrentTenantService
{
    Guid? OrganizationId { get; }
}
