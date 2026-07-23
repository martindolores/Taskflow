namespace TaskFlow.Domain.Entities;

public class Project
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Organization? Organization { get; set; }

    public required string Name { get; set; }

    public required string Color { get; set; }

    public DateTime CreatedAt { get; set; }
}
