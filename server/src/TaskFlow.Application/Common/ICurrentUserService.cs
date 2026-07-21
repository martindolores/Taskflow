using TaskFlow.Domain.Enums;

namespace TaskFlow.Application.Common;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }

    Guid? UserId { get; }

    Guid? OrganizationId { get; }

    UserRole? Role { get; }
}
