using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Entities;

public class User
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Organization? Organization { get; set; }

    public required string Email { get; set; }

    public required string PasswordHash { get; set; }

    public required string FirstName { get; set; }

    public required string LastName { get; set; }

    public UserRole Role { get; set; }

    public UserStatus Status { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
