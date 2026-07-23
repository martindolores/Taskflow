using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Activity;
using TaskFlow.Application.Activity.Dtos;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Activity;

public sealed class ActivityService(AppDbContext db) : IActivityService
{
    public async Task<IReadOnlyList<ActivityResponse>> GetActivityAsync(int limit, CancellationToken cancellationToken)
    {
        var take = Math.Clamp(limit, 1, 100);

        return await db.ActivityLog
            .OrderByDescending(a => a.CreatedAt)
            .Take(take)
            .Select(a => new ActivityResponse(
                a.Id,
                a.ActorId,
                a.Actor!.FirstName + " " + a.Actor.LastName,
                a.TaskId,
                a.Type,
                a.Summary,
                a.CreatedAt))
            .ToListAsync(cancellationToken);
    }
}
