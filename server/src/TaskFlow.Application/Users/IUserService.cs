using TaskFlow.Application.Users.Dtos;

namespace TaskFlow.Application.Users;

public interface IUserService
{
    Task<CurrentUserResponse> GetCurrentUserAsync(CancellationToken cancellationToken);
}
