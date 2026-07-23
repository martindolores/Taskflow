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
    public async Task<PagedResult<TaskListItemResponse>> GetTasksAsync(TaskListQuery query, CancellationToken cancellationToken)
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
                t.ProjectId,
                t.CreatedAt))
            .ToListAsync(cancellationToken);

        return new PagedResult<TaskListItemResponse>(items, total, page, pageSize);
    }

    public async Task<TaskResponse> GetTaskAsync(Guid taskId, CancellationToken cancellationToken)
    {
        var task = await FindTaskAsync(taskId, cancellationToken);

        return ToTaskResponse(task);
    }

    public async Task<CreateTaskResponse> CreateTaskAsync(CreateTaskRequest request, CancellationToken cancellationToken)
    {
        await EnsureValidAssigneeAsync(request.AssigneeId, cancellationToken);
        await EnsureValidProjectAsync(request.ProjectId, cancellationToken);

        var task = new TaskItem
        {
            Id = Guid.NewGuid(),
            OrganizationId = currentUserService.OrganizationId!.Value,
            Title = request.Title,
            Description = request.Description,
            Priority = request.Priority,
            AssigneeId = request.AssigneeId,
            DueDate = request.DueDate,
            ProjectId = request.ProjectId,
            CreatedById = currentUserService.UserId!.Value,
        };

        db.Tasks.Add(task);
        db.ActivityLog.Add(new ActivityLogEntry
        {
            Id = Guid.NewGuid(),
            OrganizationId = task.OrganizationId,
            ActorId = currentUserService.UserId!.Value,
            TaskId = task.Id,
            Type = ActivityType.TaskCreated,
            Summary = $"created task \"{task.Title}\"",
        });

        await db.SaveChangesAsync(cancellationToken);

        return new CreateTaskResponse(task.Id, task.Title, task.Description, task.Status, task.Priority, task.AssigneeId, task.DueDate, task.ProjectId, task.CreatedAt);
    }

    public async Task<TaskResponse> UpdateTaskAsync(Guid taskId, UpdateTaskRequest request, CancellationToken cancellationToken)
    {
        var task = await FindTaskAsync(taskId, cancellationToken);
        await EnsureValidAssigneeAsync(request.AssigneeId, cancellationToken);
        await EnsureValidProjectAsync(request.ProjectId, cancellationToken);

        var previousStatus = task.Status;
        var previousAssigneeId = task.AssigneeId;

        task.Title = request.Title;
        task.Description = request.Description;
        task.Status = request.Status;
        task.Priority = request.Priority;
        task.AssigneeId = request.AssigneeId;
        task.DueDate = request.DueDate;
        task.ProjectId = request.ProjectId;
        task.UpdatedAt = DateTime.UtcNow;

        if (task.Status != previousStatus)
        {
            AddActivityLogEntry(task, ActivityType.TaskStatusChanged, $"moved \"{task.Title}\" to {FormatStatus(task.Status)}");
        }

        if (task.AssigneeId.HasValue && task.AssigneeId != previousAssigneeId)
        {
            var assignee = await db.Users.SingleAsync(u => u.Id == task.AssigneeId.Value, cancellationToken);
            AddActivityLogEntry(task, ActivityType.TaskAssigned, $"assigned \"{task.Title}\" to {assignee.FirstName} {assignee.LastName}");
        }

        await db.SaveChangesAsync(cancellationToken);

        return ToTaskResponse(task);
    }

    public async Task<TaskStatusResponse> UpdateTaskStatusAsync(Guid taskId, UpdateTaskStatusRequest request, CancellationToken cancellationToken)
    {
        var task = await FindTaskAsync(taskId, cancellationToken);

        if (task.Status != request.Status)
        {
            AddActivityLogEntry(task, ActivityType.TaskStatusChanged, $"moved \"{task.Title}\" to {FormatStatus(request.Status)}");
        }

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

    private async Task EnsureValidProjectAsync(Guid? projectId, CancellationToken cancellationToken)
    {
        if (!projectId.HasValue)
        {
            return;
        }

        var projectExists = await db.Projects.AnyAsync(p => p.Id == projectId.Value, cancellationToken);

        if (!projectExists)
        {
            throw new InvalidProjectException(projectId.Value);
        }
    }

    private void AddActivityLogEntry(TaskItem task, ActivityType type, string summary) =>
        db.ActivityLog.Add(new ActivityLogEntry
        {
            Id = Guid.NewGuid(),
            OrganizationId = task.OrganizationId,
            ActorId = currentUserService.UserId!.Value,
            TaskId = task.Id,
            Type = type,
            Summary = summary,
        });

    private static string FormatStatus(TaskItemStatus status) => status switch
    {
        TaskItemStatus.ToDo => "To Do",
        TaskItemStatus.InProgress => "In Progress",
        TaskItemStatus.Done => "Done",
        _ => status.ToString(),
    };

    private static TaskResponse ToTaskResponse(TaskItem task) => new(
        task.Id,
        task.Title,
        task.Description,
        task.Status,
        task.Priority,
        task.AssigneeId,
        task.DueDate,
        task.ProjectId,
        task.CreatedById,
        task.CreatedAt,
        task.UpdatedAt);
}
