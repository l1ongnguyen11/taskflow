using System;
using System.Threading.Tasks;

namespace TaskFlow.Application.Interfaces.Security;

public interface IPermissionChecker
{
    Task<bool> HasPermissionAsync(Guid userId, Guid workspaceId, string permissionKey);
    Task<Guid?> ResolveWorkspaceIdAsync(Guid? workspaceId, Guid? projectId, Guid? taskId);
}
