using System;
using System.Collections.Generic;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface IWorkspaceRepository
{
    System.Threading.Tasks.Task<Workspace?> GetByIdAsync(Guid id);
    System.Threading.Tasks.Task<Workspace?> GetBySlugAsync(string slug);
    System.Threading.Tasks.Task<bool> ExistsBySlugAsync(string slug);
    System.Threading.Tasks.Task AddAsync(Workspace workspace);
    System.Threading.Tasks.Task AddMemberAsync(WorkspaceMember member);
    System.Threading.Tasks.Task AddUserRoleAsync(UserRole userRole);
    System.Threading.Tasks.Task<Role?> GetRoleByNameAsync(string roleName);
    System.Threading.Tasks.Task AddRoleAsync(Role role);
    System.Threading.Tasks.Task<bool> IsMemberAsync(Guid workspaceId, Guid userId);
    System.Threading.Tasks.Task<bool> IsOwnerAsync(Guid workspaceId, Guid userId);
    System.Threading.Tasks.Task<(List<Workspace> Workspaces, int TotalCount)> GetMyWorkspacesAsync(Guid userId, int limit, int offset);
    System.Threading.Tasks.Task SaveChangesAsync();

    // Member & Invitation Methods
    System.Threading.Tasks.Task<WorkspaceMember?> GetMemberAsync(Guid workspaceId, Guid userId);
    System.Threading.Tasks.Task<List<WorkspaceMember>> GetMembersAsync(Guid workspaceId, string? search = null, int limit = 20, int offset = 0);
    System.Threading.Tasks.Task<int> GetMembersCountAsync(Guid workspaceId, string? search = null);
    System.Threading.Tasks.Task<Invitation?> GetInvitationByTokenAsync(string token);
    System.Threading.Tasks.Task<Invitation?> GetInvitationByEmailAndWorkspaceAsync(string email, Guid workspaceId);
    System.Threading.Tasks.Task AddInvitationAsync(Invitation invitation);
    System.Threading.Tasks.Task<List<UserRole>> GetUserRolesAsync(Guid workspaceId, Guid userId);
    void RemoveMember(WorkspaceMember member);
    void RemoveUserRole(UserRole userRole);
    System.Threading.Tasks.Task<List<UserRole>> GetWorkspaceUserRolesAsync(Guid workspaceId, List<Guid> userIds);
    System.Threading.Tasks.Task<Invitation?> GetInvitationByIdAsync(Guid id);
    System.Threading.Tasks.Task<(List<Invitation> Invitations, int TotalCount)> GetWorkspaceInvitationsAsync(Guid workspaceId, string? status, int limit, int offset);
    System.Threading.Tasks.Task<List<Invitation>> GetUserInvitationsAsync(string email);
}
