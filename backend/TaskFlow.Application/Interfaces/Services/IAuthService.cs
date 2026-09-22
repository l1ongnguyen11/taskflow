using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Auth;

namespace TaskFlow.Application.Interfaces.Services;

public interface IAuthService
{
    System.Threading.Tasks.Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request);

    System.Threading.Tasks.Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request);

    System.Threading.Tasks.Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request);

    System.Threading.Tasks.Task<ApiResponse<object>> LogoutAsync(Guid userId, string refreshToken);
}
