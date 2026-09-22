using System;
using System.Collections.Generic;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Authorization;

namespace TaskFlow.Application.Interfaces.Services;

public interface IAuthorizationService
{
    System.Threading.Tasks.Task<ApiResponse<List<RoleResponse>>> GetRolesAsync(Guid userId);
    System.Threading.Tasks.Task<ApiResponse<RoleResponse>> GetRoleByIdAsync(Guid userId, Guid roleId);
    System.Threading.Tasks.Task<ApiResponse<RoleResponse>> CreateRoleAsync(Guid userId, CreateRoleRequest request);
    System.Threading.Tasks.Task<ApiResponse<RoleResponse>> UpdateRoleAsync(Guid userId, Guid roleId, UpdateRoleRequest request);
    System.Threading.Tasks.Task<ApiResponse<object>> DeleteRoleAsync(Guid userId, Guid roleId);

    System.Threading.Tasks.Task<ApiResponse<List<PermissionResponse>>> GetPermissionsAsync(Guid userId);
    System.Threading.Tasks.Task<ApiResponse<PermissionResponse>> GetPermissionByIdAsync(Guid userId, Guid permissionId);
    System.Threading.Tasks.Task<ApiResponse<PermissionResponse>> CreatePermissionAsync(Guid userId, CreatePermissionRequest request);
    System.Threading.Tasks.Task<ApiResponse<PermissionResponse>> UpdatePermissionAsync(Guid userId, Guid permissionId, UpdatePermissionRequest request);
    System.Threading.Tasks.Task<ApiResponse<object>> DeletePermissionAsync(Guid userId, Guid permissionId);

    System.Threading.Tasks.Task<ApiResponse<List<UserRoleResponse>>> GetWorkspaceUserRolesAsync(Guid userId, Guid workspaceId);
    System.Threading.Tasks.Task<ApiResponse<UserRoleResponse>> AssignUserRoleAsync(Guid userId, Guid workspaceId, AssignUserRoleRequest request);
    System.Threading.Tasks.Task<ApiResponse<object>> RemoveUserRoleAsync(Guid userId, Guid workspaceId, Guid targetUserId, Guid roleId);
    System.Threading.Tasks.Task<ApiResponse<List<string>>> GetMyPermissionsAsync(Guid userId, Guid workspaceId);

    System.Threading.Tasks.Task<ApiResponse<List<RolePermissionResponse>>> GetRolePermissionsAsync(Guid userId, Guid roleId);
    System.Threading.Tasks.Task<ApiResponse<RolePermissionResponse>> AssignRolePermissionAsync(Guid userId, Guid roleId, AssignRolePermissionRequest request);
    System.Threading.Tasks.Task<ApiResponse<object>> RemoveRolePermissionAsync(Guid userId, Guid roleId, Guid permissionId);
}
