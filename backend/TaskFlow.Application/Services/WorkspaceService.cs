using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Activities;
using TaskFlow.Application.DTOs.Workspaces;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Services;

public class WorkspaceService : IWorkspaceService
{
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IActivityService _activityService;

    public WorkspaceService(
        IWorkspaceRepository workspaceRepository,
        IActivityService activityService)
    {
        _workspaceRepository = workspaceRepository;
        _activityService = activityService;
    }

    public async Task<ApiResponse<WorkspaceResponse>> CreateWorkspaceAsync(Guid userId, CreateWorkspaceRequest request)
    {
        var slug = request.Slug.Trim().ToLowerInvariant();
        if (await _workspaceRepository.ExistsBySlugAsync(slug))
        {
            return ApiResponse<WorkspaceResponse>.FailResponse("Workspace slug is already in use.");
        }

        var workspace = new Workspace
        {
            Id = Guid.NewGuid(),
            Name = request.Name.Trim(),
            Slug = slug,
            Description = request.Description?.Trim(),
            CreatedBy = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _workspaceRepository.AddAsync(workspace);

        // Add member
        var member = new WorkspaceMember
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspace.Id,
            UserId = userId,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _workspaceRepository.AddMemberAsync(member);

        // Ensure owner role exists
        var ownerRole = await _workspaceRepository.GetRoleByNameAsync("owner");
        if (ownerRole == null)
        {
            ownerRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = "owner",
                Description = "Workspace Owner",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _workspaceRepository.AddRoleAsync(ownerRole);
        }

        // Add user role mapping for this workspace
        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = ownerRole.Id,
            WorkspaceId = workspace.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _workspaceRepository.AddUserRoleAsync(userRole);

        await _workspaceRepository.SaveChangesAsync();

        try
        {
            await _activityService.LogActivityAsync(userId, workspace.Id, new LogActivityRequest
            {
                EntityType = "Workspace",
                EntityId = workspace.Id,
                Action = "created",
                NewValue = workspace.Name
            });
        }
        catch { }

        var response = MapToWorkspaceResponse(workspace);
        return ApiResponse<WorkspaceResponse>.SuccessResponse(response, "Workspace created successfully.");
    }

    public async Task<ApiResponse<WorkspaceResponse>> UpdateWorkspaceAsync(Guid userId, Guid workspaceId, UpdateWorkspaceRequest request)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            return ApiResponse<WorkspaceResponse>.FailResponse("Workspace not found.");
        }

        // Verify permissions - Must be Owner or Admin
        var isOwner = await _workspaceRepository.IsOwnerAsync(workspaceId, userId) || workspace.CreatedBy == userId;
        if (!isOwner)
        {
            var userRoles = await _workspaceRepository.GetUserRolesAsync(workspaceId, userId);
            var isAdmin = userRoles.Any(ur => ur.Role.Name.Equals("admin", StringComparison.OrdinalIgnoreCase) || 
                                              ur.Role.Name.Equals("owner", StringComparison.OrdinalIgnoreCase));
            if (!isAdmin)
            {
                return ApiResponse<WorkspaceResponse>.FailResponse("You do not have permission to manage this workspace.");
            }
        }

        var oldName = workspace.Name;
        workspace.Name = request.Name.Trim();
        workspace.Description = request.Description?.Trim();
        if (request.LogoUrl != null)
        {
            workspace.LogoUrl = string.IsNullOrWhiteSpace(request.LogoUrl) ? null : request.LogoUrl.Trim();
        }
        workspace.UpdatedAt = DateTime.UtcNow;

        await _workspaceRepository.SaveChangesAsync();

        try
        {
            await _activityService.LogActivityAsync(userId, workspaceId, new LogActivityRequest
            {
                EntityType = "Workspace",
                EntityId = workspaceId,
                Action = "updated",
                OldValue = oldName,
                NewValue = workspace.Name
            });
        }
        catch { }

        var memberCount = await _workspaceRepository.GetMembersCountAsync(workspaceId);
        var response = MapToWorkspaceResponse(workspace, memberCount);
        return ApiResponse<WorkspaceResponse>.SuccessResponse(response, "Workspace updated successfully.");
    }

    public async Task<ApiResponse<object>> DeleteWorkspaceAsync(Guid userId, Guid workspaceId)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            return ApiResponse<object>.FailResponse("Workspace not found.");
        }

        // Verify permissions - Only Owner role can delete workspace
        var isOwner = await _workspaceRepository.IsOwnerAsync(workspaceId, userId);
        if (!isOwner)
        {
            return ApiResponse<object>.FailResponse("Only the workspace owner can delete the workspace.");
        }

        workspace.DeletedAt = DateTime.UtcNow;
        workspace.UpdatedAt = DateTime.UtcNow;

        await _workspaceRepository.SaveChangesAsync();

        try
        {
            await _activityService.LogActivityAsync(userId, workspaceId, new LogActivityRequest
            {
                EntityType = "Workspace",
                EntityId = workspaceId,
                Action = "deleted",
                OldValue = workspace.Name
            });
        }
        catch { }

        return ApiResponse<object>.SuccessResponse(new { }, "Workspace deleted successfully.");
    }

    public async Task<ApiResponse<WorkspaceResponse>> GetWorkspaceAsync(Guid userId, Guid workspaceId)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            return ApiResponse<WorkspaceResponse>.FailResponse("Workspace not found.");
        }

        // Verify member status
        var isMember = await _workspaceRepository.IsMemberAsync(workspaceId, userId);
        if (!isMember)
        {
            return ApiResponse<WorkspaceResponse>.FailResponse("You do not have access to this workspace.");
        }

        var response = MapToWorkspaceResponse(workspace);
        return ApiResponse<WorkspaceResponse>.SuccessResponse(response, "Workspace retrieved successfully.");
    }

    public async Task<PaginatedResponse<WorkspaceResponse>> GetMyWorkspacesAsync(Guid userId, int limit, int offset)
    {
        var (workspaces, totalCount) = await _workspaceRepository.GetMyWorkspacesAsync(userId, limit, offset);

        var list = new List<WorkspaceResponse>();
        foreach (var w in workspaces)
        {
            var memberCount = await _workspaceRepository.GetMembersCountAsync(w.Id);
            list.Add(MapToWorkspaceResponse(w, memberCount));
        }

        return PaginatedResponse<WorkspaceResponse>.SuccessResponse(list, totalCount, limit, offset, "My workspaces retrieved successfully.");
    }

    private WorkspaceResponse MapToWorkspaceResponse(Workspace w, int memberCount = 0)
    {
        return new WorkspaceResponse
        {
            Id = w.Id,
            Name = w.Name,
            Slug = w.Slug,
            Description = w.Description,
            LogoUrl = w.LogoUrl,
            CreatedBy = w.CreatedBy,
            CreatedAt = w.CreatedAt,
            MemberCount = memberCount
        };
    }
}
