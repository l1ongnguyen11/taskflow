using System;
using System.Collections.Generic;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface IAuthorizationRepository
{
    System.Threading.Tasks.Task<List<Role>> GetAllRolesAsync();
    System.Threading.Tasks.Task<Role?> GetRoleByIdAsync(Guid id);
    System.Threading.Tasks.Task<Role?> GetRoleByNameAsync(string name);
    System.Threading.Tasks.Task AddRoleAsync(Role role);
    System.Threading.Tasks.Task<List<Permission>> GetAllPermissionsAsync();
    System.Threading.Tasks.Task<Permission?> GetPermissionByIdAsync(Guid id);
    System.Threading.Tasks.Task<Permission?> GetPermissionByKeyAsync(string key);
    System.Threading.Tasks.Task AddPermissionAsync(Permission permission);
    System.Threading.Tasks.Task<List<UserRole>> GetUserRolesByWorkspaceAsync(Guid workspaceId);
    System.Threading.Tasks.Task<UserRole?> GetUserRoleAsync(Guid workspaceId, Guid userId, Guid roleId);
    System.Threading.Tasks.Task AddUserRoleAsync(UserRole userRole);
    void RemoveUserRole(UserRole userRole);
    System.Threading.Tasks.Task<List<RolePermission>> GetRolePermissionsAsync(Guid roleId);
    System.Threading.Tasks.Task<RolePermission?> GetRolePermissionAsync(Guid roleId, Guid permissionId);
    System.Threading.Tasks.Task AddRolePermissionAsync(RolePermission rolePermission);
    void RemoveRolePermission(RolePermission rolePermission);
    System.Threading.Tasks.Task<List<string>> GetPermissionKeysByRoleIdsAsync(List<Guid> roleIds);
    System.Threading.Tasks.Task SaveChangesAsync();
}
