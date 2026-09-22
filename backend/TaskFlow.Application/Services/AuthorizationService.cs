using System;
using System.Collections.Generic;
using System.Linq;
using Task = System.Threading.Tasks.Task;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Authorization;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Services;

public class AuthorizationService : IAuthorizationService
{
    private static readonly HashSet<string> SystemRoles = new(StringComparer.OrdinalIgnoreCase)
    {
        "owner", "admin", "member", "viewer"
    };

    private readonly IAuthorizationRepository _authorizationRepository;
    private readonly IWorkspaceRepository _workspaceRepository;

    public AuthorizationService(
        IAuthorizationRepository authorizationRepository,
        IWorkspaceRepository workspaceRepository)
    {
        _authorizationRepository = authorizationRepository;
        _workspaceRepository = workspaceRepository;
    }

    public async Task<ApiResponse<List<RoleResponse>>> GetRolesAsync(Guid userId)
    {
        _ = userId;
        var roles = await _authorizationRepository.GetAllRolesAsync();
        return ApiResponse<List<RoleResponse>>.SuccessResponse(
            roles.Select(MapToRoleResponse).ToList(), "Roles retrieved successfully.");
    }

    public async Task<ApiResponse<RoleResponse>> GetRoleByIdAsync(Guid userId, Guid roleId)
    {
        _ = userId;
        var role = await _authorizationRepository.GetRoleByIdAsync(roleId);
        if (role == null)
        {
            return ApiResponse<RoleResponse>.FailResponse("Role not found.");
        }

        return ApiResponse<RoleResponse>.SuccessResponse(MapToRoleResponse(role), "Role retrieved successfully.");
    }

    public async Task<ApiResponse<RoleResponse>> CreateRoleAsync(Guid userId, CreateRoleRequest request)
    {
        if (!await IsGlobalAdminAsync(userId))
        {
            return ApiResponse<RoleResponse>.FailResponse("You do not have permission to manage roles.");
        }

        var name = request.Name.Trim().ToLowerInvariant();
        var existing = await _authorizationRepository.GetRoleByNameAsync(name);
        if (existing != null)
        {
            return ApiResponse<RoleResponse>.FailResponse("A role with this name already exists.");
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            Name = name,
            Description = request.Description?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _authorizationRepository.AddRoleAsync(role);
        await _authorizationRepository.SaveChangesAsync();

        return ApiResponse<RoleResponse>.SuccessResponse(MapToRoleResponse(role), "Role created successfully.");
    }

    public async Task<ApiResponse<RoleResponse>> UpdateRoleAsync(Guid userId, Guid roleId, UpdateRoleRequest request)
    {
        if (!await IsGlobalAdminAsync(userId))
        {
            return ApiResponse<RoleResponse>.FailResponse("You do not have permission to manage roles.");
        }

        var role = await _authorizationRepository.GetRoleByIdAsync(roleId);
        if (role == null)
        {
            return ApiResponse<RoleResponse>.FailResponse("Role not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            if (SystemRoles.Contains(role.Name))
            {
                return ApiResponse<RoleResponse>.FailResponse("System roles cannot be renamed.");
            }

            role.Name = request.Name.Trim().ToLowerInvariant();
        }

        if (request.Description != null)
        {
            role.Description = string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim();
        }

        role.UpdatedAt = DateTime.UtcNow;
        await _authorizationRepository.SaveChangesAsync();

        return ApiResponse<RoleResponse>.SuccessResponse(MapToRoleResponse(role), "Role updated successfully.");
    }

    public async Task<ApiResponse<object>> DeleteRoleAsync(Guid userId, Guid roleId)
    {
        if (!await IsGlobalAdminAsync(userId))
        {
            return ApiResponse<object>.FailResponse("You do not have permission to manage roles.");
        }

        var role = await _authorizationRepository.GetRoleByIdAsync(roleId);
        if (role == null)
        {
            return ApiResponse<object>.FailResponse("Role not found.");
        }

        if (SystemRoles.Contains(role.Name))
        {
            return ApiResponse<object>.FailResponse("System roles cannot be deleted.");
        }

        role.DeletedAt = DateTime.UtcNow;
        role.UpdatedAt = DateTime.UtcNow;
        await _authorizationRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Role deleted successfully.");
    }

    public async Task<ApiResponse<List<PermissionResponse>>> GetPermissionsAsync(Guid userId)
    {
        _ = userId;
        var permissions = await _authorizationRepository.GetAllPermissionsAsync();
        return ApiResponse<List<PermissionResponse>>.SuccessResponse(
            permissions.Select(MapToPermissionResponse).ToList(), "Permissions retrieved successfully.");
    }

    public async Task<ApiResponse<PermissionResponse>> GetPermissionByIdAsync(Guid userId, Guid permissionId)
    {
        _ = userId;
        var permission = await _authorizationRepository.GetPermissionByIdAsync(permissionId);
        if (permission == null)
        {
            return ApiResponse<PermissionResponse>.FailResponse("Permission not found.");
        }

        return ApiResponse<PermissionResponse>.SuccessResponse(
            MapToPermissionResponse(permission), "Permission retrieved successfully.");
    }

    public async Task<ApiResponse<PermissionResponse>> CreatePermissionAsync(Guid userId, CreatePermissionRequest request)
    {
        if (!await IsGlobalAdminAsync(userId))
        {
            return ApiResponse<PermissionResponse>.FailResponse("You do not have permission to manage permissions.");
        }

        var key = request.Key.Trim().ToLowerInvariant();
        var existing = await _authorizationRepository.GetPermissionByKeyAsync(key);
        if (existing != null)
        {
            return ApiResponse<PermissionResponse>.FailResponse("A permission with this key already exists.");
        }

        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            Key = key,
            Description = request.Description?.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _authorizationRepository.AddPermissionAsync(permission);
        await _authorizationRepository.SaveChangesAsync();

        return ApiResponse<PermissionResponse>.SuccessResponse(
            MapToPermissionResponse(permission), "Permission created successfully.");
    }

    public async Task<ApiResponse<PermissionResponse>> UpdatePermissionAsync(
        Guid userId, Guid permissionId, UpdatePermissionRequest request)
    {
        if (!await IsGlobalAdminAsync(userId))
        {
            return ApiResponse<PermissionResponse>.FailResponse("You do not have permission to manage permissions.");
        }

        var permission = await _authorizationRepository.GetPermissionByIdAsync(permissionId);
        if (permission == null)
        {
            return ApiResponse<PermissionResponse>.FailResponse("Permission not found.");
        }

        if (!string.IsNullOrWhiteSpace(request.Key))
        {
            permission.Key = request.Key.Trim().ToLowerInvariant();
        }

        if (request.Description != null)
        {
            permission.Description = string.IsNullOrWhiteSpace(request.Description)
                ? null
                : request.Description.Trim();
        }

        permission.UpdatedAt = DateTime.UtcNow;
        await _authorizationRepository.SaveChangesAsync();

        return ApiResponse<PermissionResponse>.SuccessResponse(
            MapToPermissionResponse(permission), "Permission updated successfully.");
    }

    public async Task<ApiResponse<object>> DeletePermissionAsync(Guid userId, Guid permissionId)
    {
        if (!await IsGlobalAdminAsync(userId))
        {
            return ApiResponse<object>.FailResponse("You do not have permission to manage permissions.");
        }

        var permission = await _authorizationRepository.GetPermissionByIdAsync(permissionId);
        if (permission == null)
        {
            return ApiResponse<object>.FailResponse("Permission not found.");
        }

        permission.DeletedAt = DateTime.UtcNow;
        permission.UpdatedAt = DateTime.UtcNow;
        await _authorizationRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Permission deleted successfully.");
    }

    public async Task<ApiResponse<List<UserRoleResponse>>> GetWorkspaceUserRolesAsync(Guid userId, Guid workspaceId)
    {
        var accessError = await EnsureWorkspaceMemberAsync(userId, workspaceId);
        if (accessError != null)
        {
            return ApiResponse<List<UserRoleResponse>>.FailResponse(accessError);
        }

        var userRoles = await _authorizationRepository.GetUserRolesByWorkspaceAsync(workspaceId);
        return ApiResponse<List<UserRoleResponse>>.SuccessResponse(
            userRoles.Select(MapToUserRoleResponse).ToList(), "User roles retrieved successfully.");
    }

    public async Task<ApiResponse<UserRoleResponse>> AssignUserRoleAsync(
        Guid userId, Guid workspaceId, AssignUserRoleRequest request)
    {
        if (!await IsWorkspaceOwnerOrAdminAsync(workspaceId, userId))
        {
            return ApiResponse<UserRoleResponse>.FailResponse("You do not have permission to assign roles.");
        }

        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            return ApiResponse<UserRoleResponse>.FailResponse("Workspace not found.");
        }

        var isMember = await _workspaceRepository.IsMemberAsync(workspaceId, request.UserId);
        if (!isMember)
        {
            return ApiResponse<UserRoleResponse>.FailResponse("User is not a member of this workspace.");
        }

        var role = await _authorizationRepository.GetRoleByIdAsync(request.RoleId);
        if (role == null)
        {
            return ApiResponse<UserRoleResponse>.FailResponse("Role not found.");
        }

        var existingRoles = await _workspaceRepository.GetUserRolesAsync(workspaceId, request.UserId);
        foreach (var existing in existingRoles)
        {
            _workspaceRepository.RemoveUserRole(existing);
        }

        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = request.UserId,
            RoleId = request.RoleId,
            WorkspaceId = workspaceId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _authorizationRepository.AddUserRoleAsync(userRole);
        await _authorizationRepository.SaveChangesAsync();

        var reloaded = await _authorizationRepository.GetUserRoleAsync(workspaceId, request.UserId, request.RoleId);
        return ApiResponse<UserRoleResponse>.SuccessResponse(
            MapToUserRoleResponse(reloaded!), "User role assigned successfully.");
    }

    public async Task<ApiResponse<object>> RemoveUserRoleAsync(
        Guid userId, Guid workspaceId, Guid targetUserId, Guid roleId)
    {
        if (!await IsWorkspaceOwnerOrAdminAsync(workspaceId, userId))
        {
            return ApiResponse<object>.FailResponse("You do not have permission to remove roles.");
        }

        var userRole = await _authorizationRepository.GetUserRoleAsync(workspaceId, targetUserId, roleId);
        if (userRole == null)
        {
            return ApiResponse<object>.FailResponse("User role not found.");
        }

        if (userRole.Role.Name.Equals("owner", StringComparison.OrdinalIgnoreCase))
        {
            return ApiResponse<object>.FailResponse("Cannot remove the owner role.");
        }

        _authorizationRepository.RemoveUserRole(userRole);
        await _authorizationRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "User role removed successfully.");
    }

    public async Task<ApiResponse<List<string>>> GetMyPermissionsAsync(Guid userId, Guid workspaceId)
    {
        var accessError = await EnsureWorkspaceMemberAsync(userId, workspaceId);
        if (accessError != null)
        {
            return ApiResponse<List<string>>.FailResponse(accessError);
        }

        var userRoles = await _workspaceRepository.GetUserRolesAsync(workspaceId, userId);
        var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
        var permissions = roleIds.Count == 0
            ? new List<string>()
            : await _authorizationRepository.GetPermissionKeysByRoleIdsAsync(roleIds);

        return ApiResponse<List<string>>.SuccessResponse(permissions, "Permissions retrieved successfully.");
    }

    public async Task<ApiResponse<List<RolePermissionResponse>>> GetRolePermissionsAsync(Guid userId, Guid roleId)
    {
        if (!await IsGlobalAdminAsync(userId))
        {
            return ApiResponse<List<RolePermissionResponse>>.FailResponse("You do not have permission to view role permissions.");
        }

        var role = await _authorizationRepository.GetRoleByIdAsync(roleId);
        if (role == null)
        {
            return ApiResponse<List<RolePermissionResponse>>.FailResponse("Role not found.");
        }

        var rolePermissions = await _authorizationRepository.GetRolePermissionsAsync(roleId);
        return ApiResponse<List<RolePermissionResponse>>.SuccessResponse(
            rolePermissions.Select(MapToRolePermissionResponse).ToList(),
            "Role permissions retrieved successfully.");
    }

    public async Task<ApiResponse<RolePermissionResponse>> AssignRolePermissionAsync(
        Guid userId, Guid roleId, AssignRolePermissionRequest request)
    {
        if (!await IsGlobalAdminAsync(userId))
        {
            return ApiResponse<RolePermissionResponse>.FailResponse("You do not have permission to assign role permissions.");
        }

        var role = await _authorizationRepository.GetRoleByIdAsync(roleId);
        if (role == null)
        {
            return ApiResponse<RolePermissionResponse>.FailResponse("Role not found.");
        }

        var permission = await _authorizationRepository.GetPermissionByIdAsync(request.PermissionId);
        if (permission == null)
        {
            return ApiResponse<RolePermissionResponse>.FailResponse("Permission not found.");
        }

        var existing = await _authorizationRepository.GetRolePermissionAsync(roleId, request.PermissionId);
        if (existing != null)
        {
            return ApiResponse<RolePermissionResponse>.FailResponse("Permission is already assigned to this role.");
        }

        var rolePermission = new RolePermission
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            PermissionId = request.PermissionId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _authorizationRepository.AddRolePermissionAsync(rolePermission);
        await _authorizationRepository.SaveChangesAsync();

        var reloaded = await _authorizationRepository.GetRolePermissionAsync(roleId, request.PermissionId);
        return ApiResponse<RolePermissionResponse>.SuccessResponse(
            MapToRolePermissionResponse(reloaded!), "Role permission assigned successfully.");
    }

    public async Task<ApiResponse<object>> RemoveRolePermissionAsync(Guid userId, Guid roleId, Guid permissionId)
    {
        if (!await IsGlobalAdminAsync(userId))
        {
            return ApiResponse<object>.FailResponse("You do not have permission to remove role permissions.");
        }

        var rolePermission = await _authorizationRepository.GetRolePermissionAsync(roleId, permissionId);
        if (rolePermission == null)
        {
            return ApiResponse<object>.FailResponse("Role permission not found.");
        }

        _authorizationRepository.RemoveRolePermission(rolePermission);
        await _authorizationRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Role permission removed successfully.");
    }

    private async Task<bool> IsGlobalAdminAsync(Guid userId)
    {
        var workspaces = await _workspaceRepository.GetMyWorkspacesAsync(userId, 100, 0);
        foreach (var workspace in workspaces.Workspaces)
        {
            if (await IsWorkspaceOwnerOrAdminAsync(workspace.Id, userId))
            {
                return true;
            }
        }

        return false;
    }

    private async Task<bool> IsWorkspaceOwnerOrAdminAsync(Guid workspaceId, Guid userId)
    {
        var userRoles = await _workspaceRepository.GetUserRolesAsync(workspaceId, userId);
        return userRoles.Any(ur =>
            ur.Role.Name.Equals("owner", StringComparison.OrdinalIgnoreCase) ||
            ur.Role.Name.Equals("admin", StringComparison.OrdinalIgnoreCase));
    }

    private async Task<string?> EnsureWorkspaceMemberAsync(Guid userId, Guid workspaceId)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            return "Workspace not found.";
        }

        if (!await _workspaceRepository.IsMemberAsync(workspaceId, userId))
        {
            return "You do not have access to this workspace.";
        }

        return null;
    }

    private static RoleResponse MapToRoleResponse(Role role) => new()
    {
        Id = role.Id,
        Name = role.Name,
        Description = role.Description,
        CreatedAt = role.CreatedAt,
        UpdatedAt = role.UpdatedAt
    };

    private static PermissionResponse MapToPermissionResponse(Permission permission) => new()
    {
        Id = permission.Id,
        Key = permission.Key,
        Description = permission.Description,
        CreatedAt = permission.CreatedAt,
        UpdatedAt = permission.UpdatedAt
    };

    private static UserRoleResponse MapToUserRoleResponse(UserRole userRole) => new()
    {
        Id = userRole.Id,
        UserId = userRole.UserId,
        UserDisplayName = userRole.User?.DisplayName ?? string.Empty,
        RoleId = userRole.RoleId,
        RoleName = userRole.Role?.Name ?? string.Empty,
        WorkspaceId = userRole.WorkspaceId,
        CreatedAt = userRole.CreatedAt
    };

    private static RolePermissionResponse MapToRolePermissionResponse(RolePermission rolePermission) => new()
    {
        Id = rolePermission.Id,
        RoleId = rolePermission.RoleId,
        RoleName = rolePermission.Role?.Name ?? string.Empty,
        PermissionId = rolePermission.PermissionId,
        PermissionKey = rolePermission.Permission?.Key ?? string.Empty,
        CreatedAt = rolePermission.CreatedAt
    };
}
