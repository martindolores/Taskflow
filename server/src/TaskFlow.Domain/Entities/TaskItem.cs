using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Entities;

public class TaskItem
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Organization? Organization { get; set; }

    public required string Title { get; set; }

    public string? Description { get; set; }

    public TaskItemStatus Status { get; set; } = TaskItemStatus.ToDo;

    public TaskPriority Priority { get; set; } = TaskPriority.Medium;

    public Guid? AssigneeId { get; set; }

    public User? Assignee { get; set; }

    public DateOnly? DueDate { get; set; }

    public Guid? ProjectId { get; set; }

    public Project? Project { get; set; }

    public Guid CreatedById { get; set; }

    public User? CreatedBy { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
