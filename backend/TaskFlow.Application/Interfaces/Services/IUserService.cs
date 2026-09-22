using System;
using System.IO;
using System.Threading.Tasks;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Users;

namespace TaskFlow.Application.Interfaces.Services;

public interface IUserService
{
    Task<ApiResponse<UserProfileResponse>> GetProfileAsync(Guid userId);
    Task<ApiResponse<UserProfileResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task<ApiResponse<object>> ChangePasswordAsync(Guid userId, ChangePasswordRequest request);
    Task<ApiResponse<UserProfileResponse>> UploadAvatarAsync(Guid userId, Stream fileStream, string fileName, string contentType);
    Task<ApiResponse<UserProfileResponse>> GetByIdAsync(Guid userId);
}
