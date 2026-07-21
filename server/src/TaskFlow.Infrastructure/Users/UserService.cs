using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Users;
using TaskFlow.Application.Users.Dtos;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Users;

public sealed class UserService(AppDbContext db, ICurrentUserService currentUserService) : IUserService
{
    public async Task<CurrentUserResponse> GetCurrentUserAsync(CancellationToken cancellationToken)
    {
        var user = await db.Users
            .Include(u => u.Organization)
            .SingleAsync(u => u.Id == currentUserService.UserId!.Value, cancellationToken);

        return new CurrentUserResponse(
            user.Id,
            user.Email,
            user.FirstName,
            user.LastName,
            user.Role,
            user.OrganizationId,
            user.Organization!.Name);
    }
}
