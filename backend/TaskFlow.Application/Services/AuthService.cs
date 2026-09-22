using System.Security.Cryptography;
using System.Text;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Auth;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Security;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Domain.Entities;
using Microsoft.Extensions.Configuration;

namespace TaskFlow.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenRepository _refreshTokenRepository;
    private readonly IJwtProvider _jwtProvider;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IConfiguration _configuration;

    public AuthService(
        IUserRepository userRepository,
        IRefreshTokenRepository refreshTokenRepository,
        IJwtProvider jwtProvider,
        IPasswordHasher passwordHasher,
        IConfiguration configuration)
    {
        _userRepository = userRepository;
        _refreshTokenRepository = refreshTokenRepository;
        _jwtProvider = jwtProvider;
        _passwordHasher = passwordHasher;
        _configuration = configuration;
    }

    public async System.Threading.Tasks.Task<ApiResponse<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        if (await _userRepository.ExistsByEmailAsync(request.Email))
        {
            return ApiResponse<AuthResponse>.FailResponse("Email is already registered.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = request.Email,
            PasswordHash = _passwordHasher.Hash(request.Password),
            DisplayName = request.DisplayName,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _userRepository.AddAsync(user);
        await _userRepository.SaveChangesAsync();

        return await GenerateAuthResponseAsync(user);
    }

    public async System.Threading.Tasks.Task<ApiResponse<AuthResponse>> LoginAsync(LoginRequest request)
    {
        var user = await _userRepository.GetByEmailAsync(request.Email);
        if (user == null || !user.IsActive)
        {
            return ApiResponse<AuthResponse>.FailResponse("Invalid email or password.");
        }

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
        {
            return ApiResponse<AuthResponse>.FailResponse("Invalid email or password.");
        }

        return await GenerateAuthResponseAsync(user);
    }

    public async System.Threading.Tasks.Task<ApiResponse<AuthResponse>> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var tokenHash = HashToken(request.RefreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

        if (storedToken == null || storedToken.RevokedAt != null || storedToken.ExpiresAt <= DateTime.UtcNow)
        {
            return ApiResponse<AuthResponse>.FailResponse("Invalid or expired refresh token.");
        }

        // Revoke the current token
        storedToken.RevokedAt = DateTime.UtcNow;
        await _refreshTokenRepository.SaveChangesAsync();

        var user = await _userRepository.GetByIdAsync(storedToken.UserId);
        if (user == null || !user.IsActive)
        {
            return ApiResponse<AuthResponse>.FailResponse("User is no longer active.");
        }

        return await GenerateAuthResponseAsync(user);
    }

    public async System.Threading.Tasks.Task<ApiResponse<object>> LogoutAsync(Guid userId, string refreshToken)
    {
        var tokenHash = HashToken(refreshToken);
        var storedToken = await _refreshTokenRepository.GetByTokenHashAsync(tokenHash);

        if (storedToken != null && storedToken.UserId == userId && storedToken.RevokedAt == null)
        {
            storedToken.RevokedAt = DateTime.UtcNow;
            await _refreshTokenRepository.SaveChangesAsync();
        }

        return ApiResponse<object>.SuccessResponse(new { }, "Logged out successfully.");
    }

    private async System.Threading.Tasks.Task<ApiResponse<AuthResponse>> GenerateAuthResponseAsync(User user)
    {
        var accessToken = _jwtProvider.GenerateAccessToken(user);
        var rawRefreshToken = _jwtProvider.GenerateRefreshToken();
        var hashedRefreshToken = HashToken(rawRefreshToken);

        var expireDays = int.TryParse(_configuration["Jwt:RefreshTokenExpireDays"], out var days) ? days : 7;
        var expiresAt = DateTime.UtcNow.AddDays(expireDays);

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            TokenHash = hashedRefreshToken,
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _refreshTokenRepository.AddAsync(refreshTokenEntity);
        await _refreshTokenRepository.SaveChangesAsync();

        var authResponse = new AuthResponse
        {
            AccessToken = accessToken,
            RefreshToken = rawRefreshToken,
            ExpiresAt = expiresAt
        };

        return ApiResponse<AuthResponse>.SuccessResponse(authResponse, "Authentication successful.");
    }

    private string HashToken(string token)
    {
        var bytes = Encoding.UTF8.GetBytes(token);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash);
    }
}
