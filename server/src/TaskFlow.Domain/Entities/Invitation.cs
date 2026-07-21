using TaskFlow.Domain.Enums;

namespace TaskFlow.Domain.Entities;

public class Invitation
{
    public Guid Id { get; set; }

    public Guid OrganizationId { get; set; }

    public Organization? Organization { get; set; }

    public required string Email { get; set; }

    public UserRole Role { get; set; }

    public required string Token { get; set; }

    public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

    public Guid InvitedById { get; set; }

    public User? InvitedBy { get; set; }

    public DateTime ExpiresAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime UpdatedAt { get; set; }
}
