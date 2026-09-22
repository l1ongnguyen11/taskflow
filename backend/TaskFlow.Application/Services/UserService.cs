using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Users;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Security;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;

    public UserService(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
    }

    public async Task<ApiResponse<UserProfileResponse>> GetProfileAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return ApiResponse<UserProfileResponse>.FailResponse("User not found or inactive.");
        }

        var profile = MapToProfileResponse(user);
        return ApiResponse<UserProfileResponse>.SuccessResponse(profile, "Profile retrieved successfully.");
    }

    public async Task<ApiResponse<UserProfileResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return ApiResponse<UserProfileResponse>.FailResponse("User not found or inactive.");
        }

        user.DisplayName = request.DisplayName.Trim();
        if (request.AvatarUrl != null)
        {
            user.AvatarUrl = request.AvatarUrl;
        }
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.SaveChangesAsync();

        var profile = MapToProfileResponse(user);
        return ApiResponse<UserProfileResponse>.SuccessResponse(profile, "Profile updated successfully.");
    }

    public async Task<ApiResponse<object>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return ApiResponse<object>.FailResponse("User not found or inactive.");
        }

        if (!_passwordHasher.Verify(request.CurrentPassword, user.PasswordHash))
        {
            return ApiResponse<object>.FailResponse("Incorrect current password.");
        }

        user.PasswordHash = _passwordHasher.Hash(request.NewPassword);
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Password changed successfully.");
    }

    public async Task<ApiResponse<UserProfileResponse>> UploadAvatarAsync(Guid userId, Stream fileStream, string fileName, string contentType)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return ApiResponse<UserProfileResponse>.FailResponse("User not found or inactive.");
        }

        // Validate file type
        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
        {
            return ApiResponse<UserProfileResponse>.FailResponse("Invalid file type. Only JPG, JPEG, PNG, and GIF are allowed.");
        }

        // Generate unique file name
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";
        var uploadFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");

        if (!Directory.Exists(uploadFolder))
        {
            Directory.CreateDirectory(uploadFolder);
        }

        var filePath = Path.Combine(uploadFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(stream);
        }

        // Set user's avatar url (using relative web path)
        user.AvatarUrl = $"/uploads/avatars/{uniqueFileName}";
        user.UpdatedAt = DateTime.UtcNow;

        await _userRepository.SaveChangesAsync();

        var profile = MapToProfileResponse(user);
        return ApiResponse<UserProfileResponse>.SuccessResponse(profile, "Avatar uploaded successfully.");
    }

    public async Task<ApiResponse<UserProfileResponse>> GetByIdAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return ApiResponse<UserProfileResponse>.FailResponse("User not found or inactive.");
        }

        var profile = MapToProfileResponse(user);
        return ApiResponse<UserProfileResponse>.SuccessResponse(profile, "User retrieved successfully.");
    }

    private UserProfileResponse MapToProfileResponse(User user)
    {
        return new UserProfileResponse
        {
            Id = user.Id,
            Email = user.Email,
            DisplayName = user.DisplayName,
            AvatarUrl = user.AvatarUrl,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt
        };
    }
}
