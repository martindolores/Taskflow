using TaskFlow.Application.Organizations.Dtos;

namespace TaskFlow.Application.Organizations;

public interface IOrganizationService
{
    Task<OrganizationResponse> GetOrganizationAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<MemberResponse>> GetMembersAsync(CancellationToken cancellationToken);

    Task<InvitationResponse> CreateInvitationAsync(CreateInvitationRequest request, CancellationToken cancellationToken);

    Task<IReadOnlyList<InvitationResponse>> GetInvitationsAsync(CancellationToken cancellationToken);

    Task RevokeInvitationAsync(Guid invitationId, CancellationToken cancellationToken);

    Task<MemberRoleResponse> UpdateMemberRoleAsync(Guid userId, UpdateMemberRoleRequest request, CancellationToken cancellationToken);

    Task DeactivateMemberAsync(Guid userId, CancellationToken cancellationToken);
}
