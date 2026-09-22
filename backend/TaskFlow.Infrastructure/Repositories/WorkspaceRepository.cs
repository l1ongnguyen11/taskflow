using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class WorkspaceRepository : IWorkspaceRepository
{
    private readonly TaskFlowDbContext _context;

    public WorkspaceRepository(TaskFlowDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<Workspace?> GetByIdAsync(Guid id)
    {
        return await _context.Workspaces
            .FirstOrDefaultAsync(x => x.Id == id && x.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<Workspace?> GetBySlugAsync(string slug)
    {
        return await _context.Workspaces
            .FirstOrDefaultAsync(x => x.Slug == slug && x.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<bool> ExistsBySlugAsync(string slug)
    {
        return await _context.Workspaces
            .AnyAsync(x => x.Slug == slug && x.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task AddAsync(Workspace workspace)
    {
        await _context.Workspaces.AddAsync(workspace);
    }

    public async System.Threading.Tasks.Task AddMemberAsync(WorkspaceMember member)
    {
        await _context.WorkspaceMembers.AddAsync(member);
    }

    public async System.Threading.Tasks.Task AddUserRoleAsync(UserRole userRole)
    {
        await _context.UserRoles.AddAsync(userRole);
    }

    public async System.Threading.Tasks.Task<Role?> GetRoleByNameAsync(string roleName)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Name == roleName);
    }

    public async System.Threading.Tasks.Task AddRoleAsync(Role role)
    {
        await _context.Roles.AddAsync(role);
    }

    public async System.Threading.Tasks.Task<bool> IsMemberAsync(Guid workspaceId, Guid userId)
    {
        return await _context.WorkspaceMembers
            .AnyAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId && m.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<bool> IsOwnerAsync(Guid workspaceId, Guid userId)
    {
        return await _context.UserRoles
            .Include(ur => ur.Role)
            .AnyAsync(ur => ur.WorkspaceId == workspaceId && 
                            ur.UserId == userId && 
                            ur.Role.Name.ToLower() == "owner" && 
                            ur.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<(List<Workspace> Workspaces, int TotalCount)> GetMyWorkspacesAsync(Guid userId, int limit, int offset)
    {
        var query = _context.WorkspaceMembers
            .Where(m => m.UserId == userId && m.DeletedAt == null)
            .Select(m => m.Workspace)
            .Where(w => w.DeletedAt == null);

        var totalCount = await query.CountAsync();
        var workspaces = await query
            .OrderByDescending(w => w.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return (workspaces, totalCount);
    }

    public async System.Threading.Tasks.Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }

    // Member & Invitation Methods
    public async System.Threading.Tasks.Task<WorkspaceMember?> GetMemberAsync(Guid workspaceId, Guid userId)
    {
        return await _context.WorkspaceMembers
            .Include(m => m.User)
            .FirstOrDefaultAsync(m => m.WorkspaceId == workspaceId && m.UserId == userId && m.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<List<WorkspaceMember>> GetMembersAsync(Guid workspaceId, string? search = null, int limit = 20, int offset = 0)
    {
        var query = _context.WorkspaceMembers
            .Include(m => m.User)
            .Where(m => m.WorkspaceId == workspaceId && m.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(m => m.User.DisplayName.ToLower().Contains(searchLower) || m.User.Email.ToLower().Contains(searchLower));
        }

        return await query
            .OrderBy(m => m.JoinedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<int> GetMembersCountAsync(Guid workspaceId, string? search = null)
    {
        var query = _context.WorkspaceMembers
            .Where(m => m.WorkspaceId == workspaceId && m.DeletedAt == null);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchLower = search.Trim().ToLower();
            query = query.Where(m => m.User.DisplayName.ToLower().Contains(searchLower) || m.User.Email.ToLower().Contains(searchLower));
        }

        return await query.CountAsync();
    }

    public async System.Threading.Tasks.Task<Invitation?> GetInvitationByTokenAsync(string token)
    {
        return await _context.Invitations
            .Include(i => i.Workspace)
            .FirstOrDefaultAsync(i => i.Token == token && i.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<Invitation?> GetInvitationByEmailAndWorkspaceAsync(string email, Guid workspaceId)
    {
        return await _context.Invitations
            .FirstOrDefaultAsync(i => i.Email.ToLower() == email.ToLower() && 
                                     i.WorkspaceId == workspaceId && 
                                     i.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task AddInvitationAsync(Invitation invitation)
    {
        await _context.Invitations.AddAsync(invitation);
    }

    public async System.Threading.Tasks.Task<List<UserRole>> GetUserRolesAsync(Guid workspaceId, Guid userId)
    {
        return await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.WorkspaceId == workspaceId && ur.UserId == userId && ur.DeletedAt == null)
            .ToListAsync();
    }

    public void RemoveMember(WorkspaceMember member)
    {
        member.DeletedAt = DateTime.UtcNow;
        member.UpdatedAt = DateTime.UtcNow;
    }

    public void RemoveUserRole(UserRole userRole)
    {
        userRole.DeletedAt = DateTime.UtcNow;
        userRole.UpdatedAt = DateTime.UtcNow;
    }

    public async System.Threading.Tasks.Task<List<UserRole>> GetWorkspaceUserRolesAsync(Guid workspaceId, List<Guid> userIds)
    {
        return await _context.UserRoles
            .Include(ur => ur.Role)
            .Where(ur => ur.WorkspaceId == workspaceId && userIds.Contains(ur.UserId) && ur.DeletedAt == null)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<Invitation?> GetInvitationByIdAsync(Guid id)
    {
        return await _context.Invitations
            .Include(i => i.Workspace)
            .FirstOrDefaultAsync(i => i.Id == id && i.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<(List<Invitation> Invitations, int TotalCount)> GetWorkspaceInvitationsAsync(Guid workspaceId, string? status, int limit, int offset)
    {
        var query = _context.Invitations
            .Include(i => i.Workspace)
            .Include(i => i.Inviter)
            .Where(i => i.WorkspaceId == workspaceId && i.DeletedAt == null);

        if (!string.IsNullOrEmpty(status))
        {
            var statusLower = status.Trim().ToLowerInvariant();
            query = query.Where(i => i.Status.ToLower() == statusLower);
        }

        var totalCount = await query.CountAsync();
        var invitations = await query
            .OrderByDescending(i => i.CreatedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync();

        return (invitations, totalCount);
    }

    public async System.Threading.Tasks.Task<List<Invitation>> GetUserInvitationsAsync(string email)
    {
        return await _context.Invitations
            .Include(i => i.Workspace)
            .Include(i => i.Inviter)
            .Where(i => i.Email.ToLower() == email.ToLower() && i.DeletedAt == null)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();
    }
}
