using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Common;
using TaskFlow.Application.Tasks;
using TaskFlow.Application.Tasks.Dtos;
using TaskFlow.Application.Tasks.Exceptions;
using TaskFlow.Domain.Entities;
using TaskFlow.Domain.Enums;
using TaskFlow.Infrastructure.Persistence;

namespace TaskFlow.Infrastructure.Tasks;

public sealed class TaskService(AppDbContext db, ICurrentUserService currentUserService) : ITaskService
{
    public async Task<TaskListResponse> GetTasksAsync(TaskListQuery query, CancellationToken cancellationToken)
    {
        var page = Math.Max(query.Page, 1);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var tasks = db.Tasks.AsQueryable();

        if (query.Status.HasValue)
        {
            tasks = tasks.Where(t => t.Status == query.Status.Value);
        }

        if (query.Priority.HasValue)
        {
            tasks = tasks.Where(t => t.Priority == query.Priority.Value);
        }

        if (query.AssigneeId.HasValue)
        {
            tasks = tasks.Where(t => t.AssigneeId == query.AssigneeId.Value);
        }

        var total = await tasks.CountAsync(cancellationToken);

        var items = await tasks
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(t => new TaskListItemResponse(
                t.Id,
                t.Title,
                t.Status,
                t.Priority,
                t.AssigneeId,
                t.Assignee == null ? null : t.Assignee.FirstName + " " + t.Assignee.LastName,
                t.DueDate,
                t.CreatedAt))
            .ToListAsync(cancellationToken);

        return new TaskListResponse(items, total, page, pageSize);
    }

    public async Task<TaskResponse> GetTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await FindTaskAsync(taskId, cancellationToken);

        return ToTaskResponse(task);
    }

    public async Task<CreateTaskResponse> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        await EnsureValidAssigneeAsync(request.AssigneeId, cancellationToken);

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            OrganizationId = currentUserService.OrganizationId!.Value,
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            AssigneeId = request.AssigneeId,
            DueDate = request.DueDate,
            CreatedById = currentUserService.UserId!.Value,
        };

        db.Tasks.Add(task);
        await db.SaveChangesAsync(cancellationToken);

        return new CreateTaskResponse(task.Id, task.Title, task.Description, task.Status, task.Priority, task.AssigneeId, task.DueDate, task.CreatedAt);
    }

    public async Task<TaskResponse> UpdateTaskAsync(Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await FindTaskAsync(taskId, cancellationToken);
        await EnsureValidAssigneeAsync(request.AssigneeId, cancellationToken);

        task.Title = request.Title;
        task.Description = request.Description;
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.AssigneeId = request.AssigneeId;
        task.DueDate = request.DueDate;
        task.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return ToTaskResponse(task);
    }

    public async Task<TaskStatusResponse> UpdateTaskStatusAsync(Guid taskId, UpdateTaskStatusRequest request, CancellationToken cancellationToken)
    {
        var task = await FindTaskAsync(taskId, cancellationToken);

        task.Status = request.Status;
        task.UpdatedAt = DateTime.UtcNow;

        await db.SaveChangesAsync(cancellationToken);

        return new TaskStatusResponse(task.Id, task.Status);
    }

    public async Task DeleteTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await FindTaskAsync(taskId, cancellationToken);

        var isAdmin = currentUserService.Role == UserRole.Admin;
        var isCreator = task.CreatedById == currentUserService.UserId;

        if (!isAdmin && !isCreator)
        {
            throw new TaskAccessForbiddenException(taskId);
        }

        db.Tasks.Remove(task);
        await db.SaveChangesAsync(cancellationToken);
    }

    private async Task<TaskItem> FindTaskAsync(Guid taskId, CancellationToken cancellationToken) =>
        await db.Tasks.SingleOrDefaultAsync(t => t.Id == taskId, cancellationToken)
            ?? throw new TaskNotFoundException(taskId);

    private async Task EnsureValidAssigneeAsync(Guid? assigneeId, CancellationToken cancellationToken)
    {
        if (!assigneeId.HasValue)
        {
            return;
        }

        var assigneeExists = await db.Users.AnyAsync(u => u.Id == assigneeId.Value, cancellationToken);

        if (!assigneeExists)
        {
            throw new InvalidAssigneeException(assigneeId.Value);
        }
    }

    private static TaskResponse ToTaskResponse(TaskItem task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.Status,
        task.Priority,
        task.AssigneeId,
        task.DueDate,
        task.CreatedById,
        task.CreatedAt,
        task.UpdatedAt);
}
