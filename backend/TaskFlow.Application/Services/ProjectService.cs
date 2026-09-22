using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Activities;
using TaskFlow.Application.DTOs.Projects;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Services;

public class ProjectService : IProjectService
{
    private readonly IProjectRepository _projectRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IUserRepository _userRepository;
    private readonly IActivityService _activityService;
    private readonly INotificationService _notificationService;

    public ProjectService(
        IProjectRepository projectRepository,
        IWorkspaceRepository workspaceRepository,
        IUserRepository userRepository,
        IActivityService activityService,
        INotificationService notificationService)
    {
        _projectRepository = projectRepository;
        _workspaceRepository = workspaceRepository;
        _userRepository = userRepository;
        _activityService = activityService;
        _notificationService = notificationService;
    }

    public async Task<ApiResponse<ProjectResponse>> CreateProjectAsync(Guid userId, Guid workspaceId, CreateProjectRequest request)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            return ApiResponse<ProjectResponse>.FailResponse("Workspace not found.");
        }

        // Check if caller is workspace member
        var isMember = await _workspaceRepository.IsMemberAsync(workspaceId, userId);
        if (!isMember)
        {
            return ApiResponse<ProjectResponse>.FailResponse("You do not have access to this workspace.");
        }

        // Validate Key pattern (alphanumeric, uppercase, 2-10 chars)
        var key = request.Key.Trim().ToUpperInvariant();
        if (key.Length < 2 || key.Length > 10 || !key.All(char.IsLetterOrDigit))
        {
            return ApiResponse<ProjectResponse>.FailResponse("Project Key must be alphanumeric and between 2 and 10 characters.");
        }

        // Check unique Key in Workspace
        var existingProject = await _projectRepository.GetByKeyAndWorkspaceAsync(key, workspaceId);
        if (existingProject != null)
        {
            return ApiResponse<ProjectResponse>.FailResponse("Project key must be unique within this workspace.");
        }

        // Validate Lead User if provided
        User? leadUser = null;
        if (request.LeadId.HasValue)
        {
            leadUser = await _userRepository.GetByIdAsync(request.LeadId.Value);
            if (leadUser == null || !leadUser.IsActive)
            {
                return ApiResponse<ProjectResponse>.FailResponse("Lead user is invalid or inactive.");
            }

            var isLeadWorkspaceMember = await _workspaceRepository.IsMemberAsync(workspaceId, request.LeadId.Value);
            if (!isLeadWorkspaceMember)
            {
                return ApiResponse<ProjectResponse>.FailResponse("Lead user must be a member of this workspace.");
            }
        }

        // Create project
        var project = new Project
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = request.Name.Trim(),
            Key = key,
            Description = request.Description?.Trim(),
            LeadId = request.LeadId,
            IsArchived = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _projectRepository.AddAsync(project);

        // Auto-add creator as project member
        var creatorMember = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            UserId = userId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _projectRepository.AddMemberAsync(creatorMember);

        // Auto-add lead as project member if different from creator
        if (request.LeadId.HasValue && request.LeadId.Value != userId)
        {
            var leadMember = new ProjectMember
            {
                Id = Guid.NewGuid(),
                ProjectId = project.Id,
                UserId = request.LeadId.Value,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _projectRepository.AddMemberAsync(leadMember);
        }

        await _projectRepository.SaveChangesAsync();

        try
        {
            await _activityService.LogActivityAsync(userId, workspaceId, new LogActivityRequest
            {
                EntityType = "Project",
                EntityId = project.Id,
                Action = "created",
                NewValue = project.Name
            });

            if (request.LeadId.HasValue && request.LeadId.Value != userId)
            {
                await _notificationService.CreateNotificationAsync(
                    request.LeadId.Value,
                    workspaceId,
                    "project_lead_assigned",
                    "Assigned as Project Lead",
                    $"You were assigned as lead for project '{project.Name}'.",
                    "Project",
                    project.Id);
            }
        }
        catch { }

        var response = new ProjectResponse
        {
            Id = project.Id,
            WorkspaceId = project.WorkspaceId,
            Name = project.Name,
            Key = project.Key,
            Description = project.Description,
            LeadId = project.LeadId,
            LeadName = leadUser?.DisplayName ?? string.Empty,
            IsArchived = project.IsArchived,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };

        return ApiResponse<ProjectResponse>.SuccessResponse(response, "Project created successfully.");
    }

    public async Task<ApiResponse<ProjectResponse>> GetProjectByIdAsync(Guid userId, Guid projectId)
    {
        var project = await _projectRepository.GetByIdWithLeadAndMembersAsync(projectId);
        if (project == null || project.DeletedAt != null)
        {
            return ApiResponse<ProjectResponse>.FailResponse("Project not found.");
        }

        // Check if caller is workspace member
        var isMember = await _workspaceRepository.IsMemberAsync(project.WorkspaceId, userId);
        if (!isMember)
        {
            return ApiResponse<ProjectResponse>.FailResponse("You do not have access to this project.");
        }

        var response = new ProjectResponse
        {
            Id = project.Id,
            WorkspaceId = project.WorkspaceId,
            Name = project.Name,
            Key = project.Key,
            Description = project.Description,
            LeadId = project.LeadId,
            LeadName = project.Lead?.DisplayName ?? string.Empty,
            IsArchived = project.IsArchived,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };

        return ApiResponse<ProjectResponse>.SuccessResponse(response, "Project retrieved successfully.");
    }

    public async Task<ApiResponse<ProjectResponse>> UpdateProjectAsync(Guid userId, Guid projectId, UpdateProjectRequest request)
    {
        var project = await _projectRepository.GetByIdWithLeadAndMembersAsync(projectId);
        if (project == null || project.DeletedAt != null)
        {
            return ApiResponse<ProjectResponse>.FailResponse("Project not found.");
        }

        // Check authorization (Owner, Admin or Project Lead)
        var userRoles = await _workspaceRepository.GetUserRolesAsync(project.WorkspaceId, userId);
        var isAuthorized = userRoles.Any(ur => ur.Role.Name.ToLower() == "owner" || ur.Role.Name.ToLower() == "admin") || project.LeadId == userId;
        if (!isAuthorized)
        {
            return ApiResponse<ProjectResponse>.FailResponse("You do not have permission to update this project.");
        }

        // Validate Lead User if provided
        User? leadUser = project.Lead;
        if (request.LeadId.HasValue && request.LeadId.Value != project.LeadId)
        {
            leadUser = await _userRepository.GetByIdAsync(request.LeadId.Value);
            if (leadUser == null || !leadUser.IsActive)
            {
                return ApiResponse<ProjectResponse>.FailResponse("Lead user is invalid or inactive.");
            }

            var isLeadWorkspaceMember = await _workspaceRepository.IsMemberAsync(project.WorkspaceId, request.LeadId.Value);
            if (!isLeadWorkspaceMember)
            {
                return ApiResponse<ProjectResponse>.FailResponse("Lead user must be a member of this workspace.");
            }

            // Ensure new lead is a member of the project
            var isProjectMember = await _projectRepository.GetMemberAsync(projectId, request.LeadId.Value);
            if (isProjectMember == null)
            {
                var newLeadMember = new ProjectMember
                {
                    Id = Guid.NewGuid(),
                    ProjectId = projectId,
                    UserId = request.LeadId.Value,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                await _projectRepository.AddMemberAsync(newLeadMember);
            }
        }
        else if (!request.LeadId.HasValue)
        {
            leadUser = null;
        }

        // Update properties
        project.Name = request.Name.Trim();
        project.Description = request.Description?.Trim();
        project.LeadId = request.LeadId;
        project.UpdatedAt = DateTime.UtcNow;

        await _projectRepository.SaveChangesAsync();

        var response = new ProjectResponse
        {
            Id = project.Id,
            WorkspaceId = project.WorkspaceId,
            Name = project.Name,
            Key = project.Key,
            Description = project.Description,
            LeadId = project.LeadId,
            LeadName = leadUser?.DisplayName ?? string.Empty,
            IsArchived = project.IsArchived,
            CreatedAt = project.CreatedAt,
            UpdatedAt = project.UpdatedAt
        };

        return ApiResponse<ProjectResponse>.SuccessResponse(response, "Project updated successfully.");
    }

    public async Task<ApiResponse<object>> DeleteProjectAsync(Guid userId, Guid projectId)
    {
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project == null)
        {
            return ApiResponse<object>.FailResponse("Project not found.");
        }

        // Only workspace Owner/Admin can delete projects
        var userRoles = await _workspaceRepository.GetUserRolesAsync(project.WorkspaceId, userId);
        var isAuthorized = userRoles.Any(ur => ur.Role.Name.ToLower() == "owner" || ur.Role.Name.ToLower() == "admin");
        if (!isAuthorized)
        {
            return ApiResponse<object>.FailResponse("Only workspace owners or admins can delete projects.");
        }

        project.DeletedAt = DateTime.UtcNow;
        project.UpdatedAt = DateTime.UtcNow;

        await _projectRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Project deleted successfully.");
    }

    public async Task<ApiResponse<object>> ArchiveProjectAsync(Guid userId, Guid projectId)
    {
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project == null)
        {
            return ApiResponse<object>.FailResponse("Project not found.");
        }

        // Workspace Owner/Admin or Project Lead can archive
        var userRoles = await _workspaceRepository.GetUserRolesAsync(project.WorkspaceId, userId);
        var isAuthorized = userRoles.Any(ur => ur.Role.Name.ToLower() == "owner" || ur.Role.Name.ToLower() == "admin") || project.LeadId == userId;
        if (!isAuthorized)
        {
            return ApiResponse<object>.FailResponse("Only workspace owners, admins, or the project lead can archive this project.");
        }

        if (project.IsArchived)
        {
            return ApiResponse<object>.FailResponse("Project is already archived.");
        }

        project.IsArchived = true;
        project.UpdatedAt = DateTime.UtcNow;

        await _projectRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Project archived successfully.");
    }

    public async Task<ApiResponse<object>> RestoreProjectAsync(Guid userId, Guid projectId)
    {
        var project = await _projectRepository.GetByIdWithLeadAndMembersAsync(projectId);
        if (project == null)
        {
            return ApiResponse<object>.FailResponse("Project not found.");
        }

        // Only workspace Owner/Admin can restore projects
        var userRoles = await _workspaceRepository.GetUserRolesAsync(project.WorkspaceId, userId);
        var isAuthorized = userRoles.Any(ur => ur.Role.Name.ToLower() == "owner" || ur.Role.Name.ToLower() == "admin");
        if (!isAuthorized)
        {
            return ApiResponse<object>.FailResponse("Only workspace owners or admins can restore projects.");
        }

        if (!project.IsArchived && project.DeletedAt == null)
        {
            return ApiResponse<object>.FailResponse("Project is not archived or deleted.");
        }

        project.IsArchived = false;
        project.DeletedAt = null;
        project.UpdatedAt = DateTime.UtcNow;

        await _projectRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Project restored successfully.");
    }

    public async Task<PaginatedResponse<ProjectResponse>> GetWorkspaceProjectsAsync(
        Guid userId, Guid workspaceId, bool includeArchived, bool includeDeleted, int limit, int offset)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            return PaginatedResponse<ProjectResponse>.FailResponse("Workspace not found.");
        }

        // Check if caller is workspace member
        var isMember = await _workspaceRepository.IsMemberAsync(workspaceId, userId);
        if (!isMember)
        {
            return PaginatedResponse<ProjectResponse>.FailResponse("You do not have access to this workspace.");
        }

        // If includeDeleted is requested, check if caller is owner or admin
        if (includeDeleted)
        {
            var userRoles = await _workspaceRepository.GetUserRolesAsync(workspaceId, userId);
            var isAuthorized = userRoles.Any(ur => ur.Role.Name.ToLower() == "owner" || ur.Role.Name.ToLower() == "admin");
            if (!isAuthorized)
            {
                // Override includeDeleted for non-admin callers
                includeDeleted = false;
            }
        }

        var (projects, totalCount) = await _projectRepository.GetWorkspaceProjectsAsync(
            workspaceId, includeArchived, includeDeleted, limit, offset);

        var list = projects.Select(p => new ProjectResponse
        {
            Id = p.Id,
            WorkspaceId = p.WorkspaceId,
            Name = p.Name,
            Key = p.Key,
            Description = p.Description,
            LeadId = p.LeadId,
            LeadName = p.Lead?.DisplayName ?? string.Empty,
            IsArchived = p.IsArchived,
            CreatedAt = p.CreatedAt,
            UpdatedAt = p.UpdatedAt
        }).ToList();

        return PaginatedResponse<ProjectResponse>.SuccessResponse(list, totalCount, limit, offset, "Projects retrieved successfully.");
    }

    public async Task<ApiResponse<ProjectMemberResponse>> AddMemberAsync(Guid userId, Guid projectId, AddProjectMemberRequest request)
    {
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project == null)
        {
            return ApiResponse<ProjectMemberResponse>.FailResponse("Project not found.");
        }

        // Check if caller is workspace owner/admin or the project lead
        var userRoles = await _workspaceRepository.GetUserRolesAsync(project.WorkspaceId, userId);
        var isAuthorized = userRoles.Any(ur => ur.Role.Name.ToLower() == "owner" || ur.Role.Name.ToLower() == "admin") || project.LeadId == userId;
        if (!isAuthorized)
        {
            return ApiResponse<ProjectMemberResponse>.FailResponse("Only project leads or workspace admins can add members to this project.");
        }

        // Validate target user
        var targetUser = await _userRepository.GetByIdAsync(request.UserId);
        if (targetUser == null || !targetUser.IsActive)
        {
            return ApiResponse<ProjectMemberResponse>.FailResponse("User not found or inactive.");
        }

        // Check if target user is workspace member
        var isWorkspaceMember = await _workspaceRepository.IsMemberAsync(project.WorkspaceId, request.UserId);
        if (!isWorkspaceMember)
        {
            return ApiResponse<ProjectMemberResponse>.FailResponse("User must be a member of the workspace before being added to the project.");
        }

        // Check if already project member
        var existingMember = await _projectRepository.GetMemberAsync(projectId, request.UserId);
        if (existingMember != null)
        {
            return ApiResponse<ProjectMemberResponse>.FailResponse("User is already a member of this project.");
        }

        var projectMember = new ProjectMember
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            UserId = request.UserId,
            Role = string.IsNullOrEmpty(request.Role) ? "member" : request.Role.Trim().ToLowerInvariant(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _projectRepository.AddMemberAsync(projectMember);
        await _projectRepository.SaveChangesAsync();

        try
        {
            await _activityService.LogActivityAsync(userId, project.WorkspaceId, new LogActivityRequest
            {
                EntityType = "ProjectMember",
                EntityId = targetUser.Id,
                Action = "member_added",
                NewValue = project.Name
            });

            if (targetUser.Id != userId)
            {
                await _notificationService.CreateNotificationAsync(
                    targetUser.Id,
                    project.WorkspaceId,
                    "project_member_added",
                    "Added to Project",
                    $"You were added to project '{project.Name}'.",
                    "Project",
                    project.Id);
            }
        }
        catch { }

        var response = new ProjectMemberResponse
        {
            UserId = targetUser.Id,
            DisplayName = targetUser.DisplayName,
            Email = targetUser.Email,
            AvatarUrl = targetUser.AvatarUrl,
            Role = projectMember.Role,
            JoinedAt = projectMember.CreatedAt
        };

        return ApiResponse<ProjectMemberResponse>.SuccessResponse(response, "Member added to project successfully.");
    }

    public async Task<ApiResponse<object>> RemoveMemberAsync(Guid userId, Guid projectId, Guid targetUserId)
    {
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project == null)
        {
            return ApiResponse<object>.FailResponse("Project not found.");
        }

        // Check if caller is workspace owner/admin or the project lead, or if removing self
        var userRoles = await _workspaceRepository.GetUserRolesAsync(project.WorkspaceId, userId);
        var isAuthorized = userRoles.Any(ur => ur.Role.Name.ToLower() == "owner" || ur.Role.Name.ToLower() == "admin") || project.LeadId == userId || userId == targetUserId;
        if (!isAuthorized)
        {
            return ApiResponse<object>.FailResponse("You do not have permission to remove members from this project.");
        }

        // Cannot remove the project lead
        if (project.LeadId == targetUserId)
        {
            return ApiResponse<object>.FailResponse("The project lead cannot be removed. Please assign a new lead first.");
        }

        var projectMember = await _projectRepository.GetMemberAsync(projectId, targetUserId);
        if (projectMember == null)
        {
            return ApiResponse<object>.FailResponse("Member not found in this project.");
        }

        _projectRepository.RemoveMember(projectMember);
        await _projectRepository.SaveChangesAsync();

        try
        {
            await _activityService.LogActivityAsync(userId, project.WorkspaceId, new LogActivityRequest
            {
                EntityType = "ProjectMember",
                EntityId = targetUserId,
                Action = "member_removed",
                OldValue = project.Name
            });

            if (targetUserId != userId)
            {
                await _notificationService.CreateNotificationAsync(
                    targetUserId,
                    project.WorkspaceId,
                    "project_member_removed",
                    "Removed from Project",
                    $"You were removed from project '{project.Name}'.",
                    "Project",
                    project.Id);
            }
        }
        catch { }

        return ApiResponse<object>.SuccessResponse(new { }, "Member removed from project successfully.");
    }

    public async Task<ApiResponse<ProjectMemberResponse>> UpdateMemberRoleAsync(Guid userId, Guid projectId, Guid targetUserId, UpdateProjectMemberRoleRequest request)
    {
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project == null)
        {
            return ApiResponse<ProjectMemberResponse>.FailResponse("Project not found.");
        }

        // Check if caller is workspace owner/admin or project lead
        var userRoles = await _workspaceRepository.GetUserRolesAsync(project.WorkspaceId, userId);
        var isAuthorized = userRoles.Any(ur => ur.Role.Name.ToLower() == "owner" || ur.Role.Name.ToLower() == "admin") || project.LeadId == userId;
        if (!isAuthorized)
        {
            return ApiResponse<ProjectMemberResponse>.FailResponse("You do not have permission to update member roles in this project.");
        }

        var projectMember = await _projectRepository.GetMemberAsync(projectId, targetUserId);
        if (projectMember == null)
        {
            return ApiResponse<ProjectMemberResponse>.FailResponse("Member not found in this project.");
        }

        var targetUser = await _userRepository.GetByIdAsync(targetUserId);
        if (targetUser == null)
        {
            return ApiResponse<ProjectMemberResponse>.FailResponse("User not found.");
        }

        projectMember.Role = string.IsNullOrEmpty(request.Role) ? "member" : request.Role.Trim().ToLowerInvariant();
        projectMember.UpdatedAt = DateTime.UtcNow;

        await _projectRepository.SaveChangesAsync();

        var response = new ProjectMemberResponse
        {
            UserId = targetUser.Id,
            DisplayName = targetUser.DisplayName,
            Email = targetUser.Email,
            AvatarUrl = targetUser.AvatarUrl,
            Role = projectMember.Role,
            JoinedAt = projectMember.CreatedAt
        };

        return ApiResponse<ProjectMemberResponse>.SuccessResponse(response, "Member role updated successfully.");
    }

    public async Task<PaginatedResponse<ProjectMemberResponse>> GetProjectMembersAsync(Guid userId, Guid projectId, int limit, int offset)
    {
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project == null)
        {
            return PaginatedResponse<ProjectMemberResponse>.FailResponse("Project not found.");
        }

        // Check if caller is workspace member
        var isMember = await _workspaceRepository.IsMemberAsync(project.WorkspaceId, userId);
        if (!isMember)
        {
            return PaginatedResponse<ProjectMemberResponse>.FailResponse("You do not have access to this project.");
        }

        var (members, totalCount) = await _projectRepository.GetProjectMembersAsync(projectId, limit, offset);

        var list = members.Select(pm => new ProjectMemberResponse
        {
            UserId = pm.UserId,
            DisplayName = pm.User.DisplayName,
            Email = pm.User.Email,
            AvatarUrl = pm.User.AvatarUrl,
            Role = pm.Role,
            JoinedAt = pm.CreatedAt
        }).ToList();

        return PaginatedResponse<ProjectMemberResponse>.SuccessResponse(list, totalCount, limit, offset, "Project members retrieved successfully.");
    }
}
