using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Domain.Entities;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class AuthorizationRepository : IAuthorizationRepository
{
    private readonly TaskFlowDbContext _context;

    public AuthorizationRepository(TaskFlowDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<List<Role>> GetAllRolesAsync()
    {
        return await _context.Roles
            .Where(r => r.DeletedAt == null)
            .OrderBy(r => r.Name)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<Role?> GetRoleByIdAsync(Guid id)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Id == id && r.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<Role?> GetRoleByNameAsync(string name)
    {
        return await _context.Roles
            .FirstOrDefaultAsync(r => r.Name.ToLower() == name.ToLower() && r.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task AddRoleAsync(Role role)
    {
        await _context.Roles.AddAsync(role);
    }

    public async System.Threading.Tasks.Task<List<Permission>> GetAllPermissionsAsync()
    {
        return await _context.Permissions
            .Where(p => p.DeletedAt == null)
            .OrderBy(p => p.Key)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<Permission?> GetPermissionByIdAsync(Guid id)
    {
        return await _context.Permissions
            .FirstOrDefaultAsync(p => p.Id == id && p.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task<Permission?> GetPermissionByKeyAsync(string key)
    {
        return await _context.Permissions
            .FirstOrDefaultAsync(p => p.Key.ToLower() == key.ToLower() && p.DeletedAt == null);
    }

    public async System.Threading.Tasks.Task AddPermissionAsync(Permission permission)
    {
        await _context.Permissions.AddAsync(permission);
    }

    public async System.Threading.Tasks.Task<List<UserRole>> GetUserRolesByWorkspaceAsync(Guid workspaceId)
    {
        return await _context.UserRoles
            .Include(ur => ur.User)
            .Include(ur => ur.Role)
            .Where(ur => ur.WorkspaceId == workspaceId)
            .OrderBy(ur => ur.User.DisplayName)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<UserRole?> GetUserRoleAsync(Guid workspaceId, Guid userId, Guid roleId)
    {
        return await _context.UserRoles
            .Include(ur => ur.User)
            .Include(ur => ur.Role)
            .FirstOrDefaultAsync(ur =>
                ur.WorkspaceId == workspaceId &&
                ur.UserId == userId &&
                ur.RoleId == roleId);
    }

    public async System.Threading.Tasks.Task AddUserRoleAsync(UserRole userRole)
    {
        await _context.UserRoles.AddAsync(userRole);
    }

    public void RemoveUserRole(UserRole userRole)
    {
        _context.UserRoles.Remove(userRole);
    }

    public async System.Threading.Tasks.Task<List<RolePermission>> GetRolePermissionsAsync(Guid roleId)
    {
        return await _context.RolePermissions
            .Include(rp => rp.Role)
            .Include(rp => rp.Permission)
            .Where(rp => rp.RoleId == roleId)
            .OrderBy(rp => rp.Permission.Key)
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task<RolePermission?> GetRolePermissionAsync(Guid roleId, Guid permissionId)
    {
        return await _context.RolePermissions
            .Include(rp => rp.Role)
            .Include(rp => rp.Permission)
            .FirstOrDefaultAsync(rp =>
                rp.RoleId == roleId &&
                rp.PermissionId == permissionId);
    }

    public async System.Threading.Tasks.Task AddRolePermissionAsync(RolePermission rolePermission)
    {
        await _context.RolePermissions.AddAsync(rolePermission);
    }

    public void RemoveRolePermission(RolePermission rolePermission)
    {
        _context.RolePermissions.Remove(rolePermission);
    }

    public async System.Threading.Tasks.Task<List<string>> GetPermissionKeysByRoleIdsAsync(List<Guid> roleIds)
    {
        return await _context.RolePermissions
            .Include(rp => rp.Permission)
            .Where(rp => roleIds.Contains(rp.RoleId) && rp.Permission.DeletedAt == null)
            .Select(rp => rp.Permission.Key)
            .Distinct()
            .ToListAsync();
    }

    public async System.Threading.Tasks.Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}
