using TaskFlow.Application.Auth.Dtos;

namespace TaskFlow.Application.Auth;

public interface IAuthService
{
    Task<RegisterResponse> RegisterOrganizationAsync(RegisterRequest request, CancellationToken cancellationToken);

    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken);

    Task<RefreshTokenResponse> RefreshTokenAsync(RefreshTokenRequest request, CancellationToken cancellationToken);

    Task LogoutAsync(LogoutRequest request, CancellationToken cancellationToken);

    Task<LoginResponse> AcceptInvitationAsync(AcceptInvitationRequest request, CancellationToken cancellationToken);
}
