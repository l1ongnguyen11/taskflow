using System;
using System.Linq;
using System.Threading.Tasks;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Security;

namespace TaskFlow.Infrastructure.Security;

public class PermissionChecker : IPermissionChecker
{
    private readonly IAuthorizationRepository _authorizationRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IBoardRepository _boardRepository;

    public PermissionChecker(
        IAuthorizationRepository authorizationRepository,
        IWorkspaceRepository workspaceRepository,
        IProjectRepository projectRepository,
        ITaskRepository taskRepository,
        IBoardRepository boardRepository)
    {
        _authorizationRepository = authorizationRepository;
        _workspaceRepository = workspaceRepository;
        _projectRepository = projectRepository;
        _taskRepository = taskRepository;
        _boardRepository = boardRepository;
    }

    public async Task<bool> HasPermissionAsync(Guid userId, Guid workspaceId, string permissionKey)
    {
        var isMember = await _workspaceRepository.IsMemberAsync(workspaceId, userId);
        if (!isMember)
        {
            return false;
        }

        var userRoles = await _workspaceRepository.GetUserRolesAsync(workspaceId, userId);
        if (userRoles.Any(ur =>
            ur.Role.Name.Equals("owner", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        var roleIds = userRoles.Select(ur => ur.RoleId).ToList();
        if (roleIds.Count == 0)
        {
            return false;
        }

        var permissions = await _authorizationRepository.GetPermissionKeysByRoleIdsAsync(roleIds);
        return permissions.Any(p => p.Equals(permissionKey, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<Guid?> ResolveWorkspaceIdAsync(Guid? workspaceId, Guid? projectId, Guid? taskId)
    {
        if (workspaceId.HasValue && workspaceId.Value != Guid.Empty)
        {
            return workspaceId.Value;
        }

        if (projectId.HasValue && projectId.Value != Guid.Empty)
        {
            var project = await _projectRepository.GetByIdAsync(projectId.Value);
            return project?.WorkspaceId;
        }

        if (taskId.HasValue && taskId.Value != Guid.Empty)
        {
            var task = await _taskRepository.GetByIdAsync(taskId.Value);
            if (task?.BoardColumnId == null)
            {
                return null;
            }

            var column = await _boardRepository.GetColumnByIdAsync(task.BoardColumnId.Value);
            if (column == null)
            {
                return null;
            }

            var project = await _projectRepository.GetByIdAsync(column.Board.ProjectId);
            return project?.WorkspaceId;
        }

        return null;
    }
}
