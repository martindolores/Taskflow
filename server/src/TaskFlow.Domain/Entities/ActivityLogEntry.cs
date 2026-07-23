using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Entities;

public class ActivityLogEntry
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Organization? Organization { get; set; }

    public Guid ActorId { get; set; }

    public User? Actor { get; set; }

    public Guid? TaskId { get; set; }

    public TaskItem? Task { get; set; }

    public ActivityType Type { get; set; }

    public required string Summary { get; set; }

    public DateTime CreatedAt { get; set; }
}
