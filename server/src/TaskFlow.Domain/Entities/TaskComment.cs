namespace TaskFlow.Domain.Entities;

public class TaskComment
{
    public Guid Id { get; set; }

    public Guid TaskId { get; set; }

    public TaskItem? Task { get; set; }

    public Guid OrganizationId { get; set; }

    public Guid AuthorId { get; set; }

    public User? Author { get; set; }

    public required string Body { get; set; }

    public DateTime CreatedAt { get; set; }
}
