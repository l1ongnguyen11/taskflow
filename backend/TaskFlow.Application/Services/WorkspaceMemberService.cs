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

public class WorkspaceMemberService : IWorkspaceMemberService
{
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IUserRepository _userRepository;
    private readonly IActivityService _activityService;
    private readonly INotificationService _notificationService;
    private readonly IEmailService _emailService;

    public WorkspaceMemberService(
        IWorkspaceRepository workspaceRepository,
        IUserRepository userRepository,
        IActivityService activityService,
        INotificationService notificationService,
        IEmailService emailService)
    {
        _workspaceRepository = workspaceRepository;
        _userRepository = userRepository;
        _activityService = activityService;
        _notificationService = notificationService;
        _emailService = emailService;
    }

    public async Task<ApiResponse<InviteMemberResponse>> InviteMemberAsync(Guid userId, Guid workspaceId, InviteMemberRequest request)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            return ApiResponse<InviteMemberResponse>.FailResponse("Workspace not found.");
        }

        // Check if current user is owner or admin
        var userRoles = await _workspaceRepository.GetUserRolesAsync(workspaceId, userId);
        var isAuthorized = userRoles.Any(ur => ur.Role.Name.ToLower() == "owner" || ur.Role.Name.ToLower() == "admin");
        if (!isAuthorized)
        {
            return ApiResponse<InviteMemberResponse>.FailResponse("Only workspace owners or admins can invite members.");
        }

        // Fetch inviter info for email template
        var inviterUser = await _userRepository.GetByIdAsync(userId);
        var inviterName = inviterUser != null && !string.IsNullOrWhiteSpace(inviterUser.DisplayName)
            ? inviterUser.DisplayName
            : "Thành viên TaskFlow";

        // Check if user to invite exists
        var inviteeUser = await _userRepository.GetByEmailAsync(request.Email);
        if (inviteeUser != null)
        {
            // Check if already a member
            var isMember = await _workspaceRepository.IsMemberAsync(workspaceId, inviteeUser.Id);
            if (isMember)
            {
                return ApiResponse<InviteMemberResponse>.FailResponse("User is already a member of this workspace.");
            }
        }

        var requestedRole = !string.IsNullOrWhiteSpace(request.Role) ? request.Role.Trim().ToLowerInvariant() : "member";

        // Check existing active invitation
        var existingInvite = await _workspaceRepository.GetInvitationByEmailAndWorkspaceAsync(request.Email, workspaceId);
        if (existingInvite != null && existingInvite.ExpiresAt > DateTime.UtcNow && existingInvite.Status == "pending")
        {
            return ApiResponse<InviteMemberResponse>.FailResponse("An active pending invitation has already been sent to this email.");
        }

        var invitation = new Invitation
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Email = request.Email.Trim().ToLowerInvariant(),
            Role = requestedRole,
            InvitedBy = userId,
            Token = Guid.NewGuid().ToString("N"),
            Status = "pending",
            ExpiresAt = DateTime.UtcNow.AddDays(7),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _workspaceRepository.AddInvitationAsync(invitation);
        await _workspaceRepository.SaveChangesAsync();

        try
        {
            await _activityService.LogActivityAsync(userId, workspaceId, new LogActivityRequest
            {
                EntityType = "Invitation",
                EntityId = invitation.Id,
                Action = "invited",
                NewValue = invitation.Email
            });

            if (inviteeUser != null && inviteeUser.Id != userId)
            {
                await _notificationService.CreateNotificationAsync(
                    inviteeUser.Id,
                    workspaceId,
                    "workspace_invitation",
                    "Workspace Invitation",
                    $"You have been invited to join workspace '{workspace.Name}'.",
                    "Workspace",
                    workspaceId);
            }

            // Send Email notification to invited user
            await _emailService.SendWorkspaceInvitationEmailAsync(
                invitation.Email,
                workspace.Name,
                inviterName,
                invitation.Token);
        }
        catch { }

        var response = new InviteMemberResponse
        {
            Id = invitation.Id,
            WorkspaceId = invitation.WorkspaceId,
            Email = invitation.Email,
            Role = invitation.Role,
            Token = invitation.Token,
            Status = invitation.Status,
            ExpiresAt = invitation.ExpiresAt
        };

        return ApiResponse<InviteMemberResponse>.SuccessResponse(response, "Invitation sent successfully.");
    }

    public async Task<ApiResponse<object>> RemoveMemberAsync(Guid userId, Guid workspaceId, Guid targetUserId)
    {
        var member = await _workspaceRepository.GetMemberAsync(workspaceId, targetUserId);
        if (member == null)
        {
            return ApiResponse<object>.FailResponse("Workspace member not found.");
        }

        // Must be owner or admin. Admin cannot remove owner. Owner cannot remove self (must transfer ownership first).
        var currentUserRoles = await _workspaceRepository.GetUserRolesAsync(workspaceId, userId);
        var targetUserRoles = await _workspaceRepository.GetUserRolesAsync(workspaceId, targetUserId);

        var isCurrentUserOwner = currentUserRoles.Any(ur => ur.Role.Name.ToLower() == "owner");
        var isCurrentUserAdmin = currentUserRoles.Any(ur => ur.Role.Name.ToLower() == "admin");
        var isTargetOwner = targetUserRoles.Any(ur => ur.Role.Name.ToLower() == "owner");

        if (userId == targetUserId)
        {
            if (isTargetOwner)
            {
                return ApiResponse<object>.FailResponse("Workspace owner cannot leave without transferring ownership first.");
            }
            // Member leaving by themselves
        }
        else
        {
            if (!isCurrentUserOwner && !isCurrentUserAdmin)
            {
                return ApiResponse<object>.FailResponse("You do not have permission to remove members.");
            }
            if (isTargetOwner)
            {
                return ApiResponse<object>.FailResponse("Workspace owner cannot be removed.");
            }
            if (isCurrentUserAdmin && targetUserRoles.Any(ur => ur.Role.Name.ToLower() == "admin"))
            {
                return ApiResponse<object>.FailResponse("Admins cannot remove other admins.");
            }
        }

        // Remove membership
        _workspaceRepository.RemoveMember(member);

        // Remove target user roles in this workspace
        foreach (var role in targetUserRoles)
        {
            _workspaceRepository.RemoveUserRole(role);
        }

        await _workspaceRepository.SaveChangesAsync();

        try
        {
            await _activityService.LogActivityAsync(userId, workspaceId, new LogActivityRequest
            {
                EntityType = "WorkspaceMember",
                EntityId = targetUserId,
                Action = "member_removed",
                OldValue = member.UserId.ToString()
            });

            if (targetUserId != userId)
            {
                await _notificationService.CreateNotificationAsync(
                    targetUserId,
                    workspaceId,
                    "workspace_member_removed",
                    "Removed from Workspace",
                    "You have been removed from the workspace.",
                    "Workspace",
                    workspaceId);
            }
        }
        catch { }

        return ApiResponse<object>.SuccessResponse(new { }, "Member removed successfully.");
    }

    public async Task<ApiResponse<object>> ChangeMemberRoleAsync(Guid userId, Guid workspaceId, Guid targetUserId, ChangeRoleRequest request)
    {
        var member = await _workspaceRepository.GetMemberAsync(workspaceId, targetUserId);
        if (member == null)
        {
            return ApiResponse<object>.FailResponse("Workspace member not found.");
        }

        // Must be owner to change roles.
        var isOwner = await _workspaceRepository.IsOwnerAsync(workspaceId, userId);
        if (!isOwner)
        {
            return ApiResponse<object>.FailResponse("Only workspace owners can change member roles.");
        }

        var targetUserRoles = await _workspaceRepository.GetUserRolesAsync(workspaceId, targetUserId);
        if (targetUserRoles.Any(ur => ur.Role.Name.ToLower() == "owner"))
        {
            return ApiResponse<object>.FailResponse("Cannot change the role of the workspace owner.");
        }

        // Remove old roles
        foreach (var ur in targetUserRoles)
        {
            _workspaceRepository.RemoveUserRole(ur);
        }

        // Fetch or create the new role
        var requestedRoleName = request.Role.Trim().ToLowerInvariant();
        var role = await _workspaceRepository.GetRoleByNameAsync(requestedRoleName);
        if (role == null)
        {
            role = new Role
            {
                Id = Guid.NewGuid(),
                Name = requestedRoleName,
                Description = $"{requestedRoleName} role",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _workspaceRepository.AddRoleAsync(role);
        }

        // Add new role mapping
        var newUserRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = targetUserId,
            RoleId = role.Id,
            WorkspaceId = workspaceId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _workspaceRepository.AddUserRoleAsync(newUserRole);

        await _workspaceRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Member role updated successfully.");
    }

    public async Task<PaginatedResponse<WorkspaceMemberResponse>> GetMembersAsync(Guid userId, Guid workspaceId, string? search, int limit, int offset)
    {
        // Must be member of the workspace to see members
        var isMember = await _workspaceRepository.IsMemberAsync(workspaceId, userId);
        if (!isMember)
        {
            return PaginatedResponse<WorkspaceMemberResponse>.FailResponse("You do not have access to this workspace.");
        }

        var members = await _workspaceRepository.GetMembersAsync(workspaceId, search, limit, offset);
        var totalCount = await _workspaceRepository.GetMembersCountAsync(workspaceId, search);

        var memberIds = members.Select(m => m.UserId).ToList();
        var userRoles = await _workspaceRepository.GetWorkspaceUserRolesAsync(workspaceId, memberIds);

        var responseList = members.Select(m =>
        {
            var roles = userRoles
                .Where(ur => ur.UserId == m.UserId)
                .Select(ur => ur.Role.Name)
                .ToList();

            return new WorkspaceMemberResponse
            {
                UserId = m.UserId,
                DisplayName = m.User.DisplayName,
                Email = m.User.Email,
                AvatarUrl = m.User.AvatarUrl,
                JoinedAt = m.JoinedAt,
                Roles = roles
            };
        }).ToList();

        return PaginatedResponse<WorkspaceMemberResponse>.SuccessResponse(responseList, totalCount, limit, offset, "Workspace members retrieved successfully.");
    }

    public async Task<ApiResponse<AcceptInvitationResponse>> AcceptInvitationAsync(Guid userId, string token)
    {
        var invitation = await _workspaceRepository.GetInvitationByTokenAsync(token);
        if (invitation == null || invitation.Status != "pending" || invitation.ExpiresAt <= DateTime.UtcNow)
        {
            return ApiResponse<AcceptInvitationResponse>.FailResponse("Invitation is invalid or expired.");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return ApiResponse<AcceptInvitationResponse>.FailResponse("User not found or inactive.");
        }

        if (invitation.Email.ToLower() != user.Email.ToLower())
        {
            return ApiResponse<AcceptInvitationResponse>.FailResponse("Invitation email does not match your account email.");
        }

        // Check if already a member
        var isMember = await _workspaceRepository.IsMemberAsync(invitation.WorkspaceId, userId);
        if (isMember)
        {
            invitation.Status = "accepted";
            await _workspaceRepository.SaveChangesAsync();

            var r = new AcceptInvitationResponse { WorkspaceId = invitation.WorkspaceId, Status = "accepted" };
            return ApiResponse<AcceptInvitationResponse>.SuccessResponse(r, "You are already a member of this workspace.");
        }

        // Accept invitation
        invitation.Status = "accepted";
        invitation.UpdatedAt = DateTime.UtcNow;

        // Add member
        var member = new WorkspaceMember
        {
            Id = Guid.NewGuid(),
            WorkspaceId = invitation.WorkspaceId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _workspaceRepository.AddMemberAsync(member);

        // Fetch or create assigned role from invitation
        var roleNameToAssign = !string.IsNullOrWhiteSpace(invitation.Role) ? invitation.Role.ToLowerInvariant() : "member";
        var assignedRole = await _workspaceRepository.GetRoleByNameAsync(roleNameToAssign);
        if (assignedRole == null)
        {
            assignedRole = new Role
            {
                Id = Guid.NewGuid(),
                Name = roleNameToAssign,
                Description = $"{roleNameToAssign} role",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _workspaceRepository.AddRoleAsync(assignedRole);
        }

        // Add user role mapping for this workspace
        var userRole = new UserRole
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            RoleId = assignedRole.Id,
            WorkspaceId = invitation.WorkspaceId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
        await _workspaceRepository.AddUserRoleAsync(userRole);

        await _workspaceRepository.SaveChangesAsync();

        try
        {
            await _activityService.LogActivityAsync(userId, invitation.WorkspaceId, new LogActivityRequest
            {
                EntityType = "Invitation",
                EntityId = invitation.Id,
                Action = "accepted",
                NewValue = user.Email
            });

            if (invitation.InvitedBy.HasValue && invitation.InvitedBy.Value != userId)
            {
                await _notificationService.CreateNotificationAsync(
                    invitation.InvitedBy.Value,
                    invitation.WorkspaceId,
                    "invitation_accepted",
                    "Invitation Accepted",
                    $"{user.DisplayName ?? user.Email} accepted your workspace invitation.",
                    "Workspace",
                    invitation.WorkspaceId);
            }
        }
        catch { }

        var response = new AcceptInvitationResponse
        {
            WorkspaceId = invitation.WorkspaceId,
            Status = "accepted"
        };

        return ApiResponse<AcceptInvitationResponse>.SuccessResponse(response, "Invitation accepted successfully.");
    }

    public async Task<ApiResponse<object>> RejectInvitationAsync(Guid userId, string token)
    {
        var invitation = await _workspaceRepository.GetInvitationByTokenAsync(token);
        if (invitation == null || invitation.Status != "pending" || invitation.ExpiresAt <= DateTime.UtcNow)
        {
            return ApiResponse<object>.FailResponse("Invitation is invalid or expired.");
        }

        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return ApiResponse<object>.FailResponse("User not found or inactive.");
        }

        if (invitation.Email.ToLower() != user.Email.ToLower())
        {
            return ApiResponse<object>.FailResponse("Invitation email does not match your account email.");
        }

        // Decline invitation
        invitation.Status = "declined";
        invitation.UpdatedAt = DateTime.UtcNow;

        await _workspaceRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Invitation rejected successfully.");
    }

    public async Task<ApiResponse<object>> CancelInvitationAsync(Guid userId, Guid workspaceId, Guid invitationId)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            return ApiResponse<object>.FailResponse("Workspace not found.");
        }

        // Check if caller is owner or admin
        var userRoles = await _workspaceRepository.GetUserRolesAsync(workspaceId, userId);
        var isAuthorized = userRoles.Any(ur => ur.Role.Name.ToLower() == "owner" || ur.Role.Name.ToLower() == "admin");

        var invitation = await _workspaceRepository.GetInvitationByIdAsync(invitationId);
        if (invitation == null)
        {
            return ApiResponse<object>.FailResponse("Invitation not found.");
        }

        if (invitation.WorkspaceId != workspaceId)
        {
            return ApiResponse<object>.FailResponse("Invitation does not belong to this workspace.");
        }

        // Owner/Admin or the inviter themselves can cancel
        if (!isAuthorized && invitation.InvitedBy != userId)
        {
            return ApiResponse<object>.FailResponse("Only workspace owners, admins, or the inviter can cancel this invitation.");
        }

        if (invitation.Status != "pending")
        {
            return ApiResponse<object>.FailResponse("Only pending invitations can be cancelled.");
        }

        // Soft delete the invitation
        invitation.DeletedAt = DateTime.UtcNow;
        invitation.UpdatedAt = DateTime.UtcNow;

        await _workspaceRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Invitation cancelled successfully.");
    }

    public async Task<PaginatedResponse<InvitationResponse>> GetWorkspaceInvitationsAsync(Guid userId, Guid workspaceId, string? status, int limit, int offset)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            return PaginatedResponse<InvitationResponse>.FailResponse("Workspace not found.");
        }

        // Must be owner or admin to view workspace invitations
        var userRoles = await _workspaceRepository.GetUserRolesAsync(workspaceId, userId);
        var isAuthorized = userRoles.Any(ur => ur.Role.Name.ToLower() == "owner" || ur.Role.Name.ToLower() == "admin");
        if (!isAuthorized)
        {
            return PaginatedResponse<InvitationResponse>.FailResponse("Only workspace owners or admins can view invitations.");
        }

        var (invitations, totalCount) = await _workspaceRepository.GetWorkspaceInvitationsAsync(workspaceId, status, limit, offset);

        var list = invitations.Select(i => new InvitationResponse
        {
            Id = i.Id,
            WorkspaceId = i.WorkspaceId,
            WorkspaceName = i.Workspace.Name,
            WorkspaceSlug = i.Workspace.Slug,
            Email = i.Email,
            Role = i.Role,
            InvitedBy = i.InvitedBy,
            InviterName = i.Inviter?.DisplayName ?? "System",
            Token = i.Token,
            Status = i.Status,
            ExpiresAt = i.ExpiresAt,
            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt
        }).ToList();

        return PaginatedResponse<InvitationResponse>.SuccessResponse(list, totalCount, limit, offset, "Workspace invitations retrieved successfully.");
    }

    public async Task<ApiResponse<List<InvitationResponse>>> GetMyInvitationsAsync(Guid userId)
    {
        var user = await _userRepository.GetByIdAsync(userId);
        if (user == null || !user.IsActive)
        {
            return ApiResponse<List<InvitationResponse>>.FailResponse("User not found or inactive.");
        }

        var invitations = await _workspaceRepository.GetUserInvitationsAsync(user.Email);

        var list = invitations.Select(i => new InvitationResponse
        {
            Id = i.Id,
            WorkspaceId = i.WorkspaceId,
            WorkspaceName = i.Workspace.Name,
            WorkspaceSlug = i.Workspace.Slug,
            Email = i.Email,
            InvitedBy = i.InvitedBy,
            InviterName = i.Inviter?.DisplayName ?? "System",
            Token = i.Token,
            Status = i.Status,
            ExpiresAt = i.ExpiresAt,
            CreatedAt = i.CreatedAt,
            UpdatedAt = i.UpdatedAt
        }).ToList();

        return ApiResponse<List<InvitationResponse>>.SuccessResponse(list, "User invitations retrieved successfully.");
    }
}
