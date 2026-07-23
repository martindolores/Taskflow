using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.TaskComments;
using TaskFlow.Application.TaskComments.Dtos;
using TaskFlow.Application.TaskComments.Exceptions;
using TaskFlow.Application.Tasks.Exceptions;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.TaskComments;

public sealed class TaskCommentService(AppDbContext db, ICurrentUserService currentUserService) : ITaskCommentService
{
    public async Task<IReadOnlyList<CommentResponse>> GetCommentsAsync(Guid taskId, CancellationToken cancellationToken)
    {
        await FindTaskAsync(taskId, cancellationToken);

        return await db.TaskComments
            .Where(c => c.TaskId == taskId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentResponse(c.Id, c.Body, c.AuthorId, c.Author!.FirstName + " " + c.Author.LastName, c.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<CommentResponse> CreateCommentAsync(Guid taskId, CreateCommentRequest request, CancellationToken cancellationToken)
    {
        var task = await FindTaskAsync(taskId, cancellationToken);
        var author = await db.Users.SingleAsync(u => u.Id == currentUserService.UserId!.Value, cancellationToken);

        var comment = new TaskComment
        {
            Id = Guid.NewGuid(),
            TaskId = task.Id,
            OrganizationId = task.OrganizationId,
            AuthorId = author.Id,
            Body = request.Body,
        };

        db.TaskComments.Add(comment);
        db.ActivityLog.Add(new ActivityLogEntry
        {
            Id = Guid.NewGuid(),
            OrganizationId = task.OrganizationId,
            ActorId = author.Id,
            TaskId = task.Id,
            Type = ActivityType.CommentAdded,
            Summary = $"commented on \"{task.Title}\"",
        });

        await db.SaveChangesAsync(cancellationToken);

        return new CommentResponse(comment.Id, comment.Body, author.Id, $"{author.FirstName} {author.LastName}", comment.CreatedAt);
    }

    public async Task DeleteCommentAsync(Guid taskId, Guid commentId, CancellationToken cancellationToken)
    {
        await FindTaskAsync(taskId, cancellationToken);

        var comment = await db.TaskComments.SingleOrDefaultAsync(c => c.Id == commentId && c.TaskId == taskId, cancellationToken)
            ?? throw new CommentNotFoundException(commentId);

        var isAdmin = currentUserService.Role == UserRole.Admin;
        var isAuthor = comment.AuthorId == currentUserService.UserId;

        if (!isAdmin && !isAuthor)
        {
            throw new CommentAccessForbiddenException(commentId);
        }

        db.TaskComments.Remove(comment);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<TaskItem> FindTaskAsync(Guid taskId, CancellationToken cancellationToken) =>
        await db.Tasks.SingleOrDefaultAsync(t => t.Id == taskId, cancellationToken)
            ?? throw new TaskNotFoundException(taskId);
}
