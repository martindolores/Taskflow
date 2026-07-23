using TaskFlow.Application.Activity.Dtos;

namespace TaskFlow.Application.Activity;

public interface IActivityService
{
    Task<IReadOnlyList<ActivityResponse>> GetActivityAsync(int limit, CancellationToken cancellationToken);
}
