using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Activities;
using TaskFlow.Application.DTOs.Tasks;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Services;

public class TaskService : ITaskService
{
    private readonly ITaskRepository _taskRepository;
    private readonly IBoardRepository _boardRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IUserRepository _userRepository;
    private readonly IActivityService _activityService;
    private readonly INotificationService _notificationService;

    public TaskService(
        ITaskRepository taskRepository,
        IBoardRepository boardRepository,
        IProjectRepository projectRepository,
        IWorkspaceRepository workspaceRepository,
        IUserRepository userRepository,
        IActivityService activityService,
        INotificationService notificationService)
    {
        _taskRepository = taskRepository;
        _boardRepository = boardRepository;
        _projectRepository = projectRepository;
        _workspaceRepository = workspaceRepository;
        _userRepository = userRepository;
        _activityService = activityService;
        _notificationService = notificationService;
    }

    private async Task<(Project? Project, bool IsMember, bool IsAdminOrLead)> GetUserProjectPermissionsAsync(Guid projectId, Guid userId)
    {
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project == null || project.DeletedAt != null)
        {
            return (null, false, false);
        }

        var isMember = await _workspaceRepository.IsMemberAsync(project.WorkspaceId, userId);
        if (!isMember)
        {
            return (project, false, false);
        }

        var userRoles = await _workspaceRepository.GetUserRolesAsync(project.WorkspaceId, userId);
        var isAdminOrLead = userRoles.Any(ur => ur.Role.Name.ToLower() == "owner" || ur.Role.Name.ToLower() == "admin") || project.LeadId == userId;

        return (project, true, isAdminOrLead);
    }

    private async Task<(TaskFlow.Domain.Entities.Task? Task, Project? Project, bool IsMember, bool IsAdminOrLead)> GetTaskPermissionsAsync(Guid taskId, Guid userId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null)
        {
            return (null, null, false, false);
        }

        var column = await _boardRepository.GetColumnByIdAsync(task.BoardColumnId ?? Guid.Empty);
        if (column == null)
        {
            return (task, null, false, false);
        }

        var (project, isMember, isAdminOrLead) = await GetUserProjectPermissionsAsync(column.Board.ProjectId, userId);
        return (task, project, isMember, isAdminOrLead);
    }

    public async Task<ApiResponse<TaskResponse>> CreateTaskAsync(Guid userId, Guid columnId, CreateTaskRequest request)
    {
        var column = await _boardRepository.GetColumnByIdAsync(columnId);
        if (column == null)
        {
            return ApiResponse<TaskResponse>.FailResponse("Board column not found.");
        }

        var (project, isMember, _) = await GetUserProjectPermissionsAsync(column.Board.ProjectId, userId);
        if (project == null)
        {
            return ApiResponse<TaskResponse>.FailResponse("Project not found.");
        }

        if (!isMember)
        {
            return ApiResponse<TaskResponse>.FailResponse("You do not have access to this project.");
        }

        // Check WIP limit
        var currentTasksCount = await _taskRepository.GetColumnTaskCountAsync(columnId);
        if (column.WipLimit.HasValue && currentTasksCount >= column.WipLimit.Value)
        {
            return ApiResponse<TaskResponse>.FailResponse("WIP Limit for this column has been reached.");
        }

        // Generate sequential Task Number per Project
        var maxTaskNumber = await _taskRepository.GetMaxTaskNumberAsync(column.Board.ProjectId);
        var taskNumber = maxTaskNumber + 1;

        // Position: find max in column and add 1000
        var tasks = await _boardRepository.GetTasksByColumnIdAsync(columnId);
        var nextPosition = tasks.Any() ? tasks.Max(t => t.Position) + 1000 : 1000;

        var task = new TaskFlow.Domain.Entities.Task
        {
            Id = Guid.NewGuid(),
            BoardColumnId = columnId,
            ReporterId = userId,
            Title = request.Title.Trim(),
            Description = request.Description?.Trim(),
            Type = string.IsNullOrEmpty(request.Type) ? "task" : request.Type.Trim().ToLowerInvariant(),
            Priority = string.IsNullOrEmpty(request.Priority) ? "medium" : request.Priority.Trim().ToLowerInvariant(),
            TaskNumber = taskNumber,
            Position = nextPosition,
            StoryPoints = request.StoryPoints,
            StartDate = request.StartDate,
            DueDate = request.DueDate,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _taskRepository.AddAsync(task);
        await _taskRepository.SaveChangesAsync();

        try
        {
            await _activityService.LogActivityAsync(userId, project.WorkspaceId, new LogActivityRequest
            {
                EntityType = "Task",
                EntityId = task.Id,
                Action = "created",
                NewValue = task.Title
            });
        }
        catch { }

        return ApiResponse<TaskResponse>.SuccessResponse(MapToTaskResponse(task), "Task created successfully.");
    }

    public async Task<PaginatedResponse<TaskResponse>> GetBoardTasksAsync(Guid userId, Guid boardId, int limit, int offset)
    {
        var board = await _boardRepository.GetByIdAsync(boardId);
        if (board == null)
        {
            return PaginatedResponse<TaskResponse>.FailResponse("Board not found.");
        }

        var (_, isMember, _) = await GetUserProjectPermissionsAsync(board.ProjectId, userId);
        if (!isMember)
        {
            return PaginatedResponse<TaskResponse>.FailResponse("You do not have access to this project.");
        }

        var allTasks = await _taskRepository.GetTasksByBoardIdAsync(boardId);
        var totalCount = allTasks.Count;
        var pagedTasks = allTasks.Skip(offset).Take(limit).Select(MapToTaskResponse).ToList();

        return PaginatedResponse<TaskResponse>.SuccessResponse(pagedTasks, totalCount, limit, offset, "Board tasks retrieved successfully.");
    }

    public async Task<ApiResponse<TaskResponse>> GetTaskByIdAsync(Guid userId, Guid taskId)
    {
        var task = await _taskRepository.GetByIdWithDetailsAsync(taskId);
        if (task == null)
        {
            return ApiResponse<TaskResponse>.FailResponse("Task not found.");
        }

        var column = task.BoardColumn;
        if (column == null)
        {
            return ApiResponse<TaskResponse>.FailResponse("Task column is invalid.");
        }

        var (_, isMember, _) = await GetUserProjectPermissionsAsync(column.Board.ProjectId, userId);
        if (!isMember)
        {
            return ApiResponse<TaskResponse>.FailResponse("You do not have access to this project.");
        }

        return ApiResponse<TaskResponse>.SuccessResponse(MapToTaskResponse(task), "Task retrieved successfully.");
    }

    public async Task<ApiResponse<TaskResponse>> UpdateTaskAsync(Guid userId, Guid taskId, UpdateTaskRequest request)
    {
        var (task, project, isMember, _) = await GetTaskPermissionsAsync(taskId, userId);
        if (task == null || project == null)
        {
            return ApiResponse<TaskResponse>.FailResponse("Task not found.");
        }

        if (!isMember)
        {
            return ApiResponse<TaskResponse>.FailResponse("You do not have access to this project.");
        }

        task.Title = request.Title.Trim();
        task.Description = request.Description?.Trim();
        if (!string.IsNullOrEmpty(request.Type))
        {
            task.Type = request.Type.Trim().ToLowerInvariant();
        }
        if (!string.IsNullOrEmpty(request.Priority))
        {
            task.Priority = request.Priority.Trim().ToLowerInvariant();
        }
        task.StoryPoints = request.StoryPoints;
        task.StartDate = request.StartDate;
        task.DueDate = request.DueDate;
        task.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.SaveChangesAsync();

        var updatedTask = await _taskRepository.GetByIdWithDetailsAsync(taskId);
        return ApiResponse<TaskResponse>.SuccessResponse(MapToTaskResponse(updatedTask!), "Task updated successfully.");
    }

    public async Task<ApiResponse<object>> DeleteTaskAsync(Guid userId, Guid taskId)
    {
        var (task, project, isMember, isAdminOrLead) = await GetTaskPermissionsAsync(taskId, userId);
        if (task == null || project == null)
        {
            return ApiResponse<object>.FailResponse("Task not found.");
        }

        if (!isMember)
        {
            return ApiResponse<object>.FailResponse("You do not have access to this project.");
        }

        var canDelete = isAdminOrLead || task.ReporterId == userId;
        if (!canDelete)
        {
            return ApiResponse<object>.FailResponse("You do not have permission to delete this task.");
        }

        task.DeletedAt = DateTime.UtcNow;
        task.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Task deleted successfully.");
    }

    public async Task<ApiResponse<TaskResponse>> MoveTaskAsync(Guid userId, Guid taskId, MoveTaskRequest request)
    {
        var (task, project, isMember, _) = await GetTaskPermissionsAsync(taskId, userId);
        if (task == null || project == null)
        {
            return ApiResponse<TaskResponse>.FailResponse("Task not found.");
        }

        if (!isMember)
        {
            return ApiResponse<TaskResponse>.FailResponse("You do not have access to this project.");
        }

        var targetColumn = await _boardRepository.GetColumnByIdAsync(request.TargetColumnId);
        if (targetColumn == null)
        {
            return ApiResponse<TaskResponse>.FailResponse("Target column not found.");
        }

        if (targetColumn.Board.ProjectId != project.Id)
        {
            return ApiResponse<TaskResponse>.FailResponse("Cannot move task across different projects.");
        }

        // Check WIP limit if changing columns
        if (task.BoardColumnId != request.TargetColumnId)
        {
            var currentTasksCount = await _taskRepository.GetColumnTaskCountAsync(request.TargetColumnId);
            if (targetColumn.WipLimit.HasValue && currentTasksCount >= targetColumn.WipLimit.Value)
            {
                return ApiResponse<TaskResponse>.FailResponse("WIP Limit for the target column has been reached.");
            }

            task.BoardColumnId = request.TargetColumnId;

            // Handle CompletedAt based on IsDoneColumn
            if (targetColumn.IsDoneColumn)
            {
                task.CompletedAt = DateTime.UtcNow;
            }
            else
            {
                task.CompletedAt = null;
            }
        }

        task.Position = request.Position;
        task.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.SaveChangesAsync();

        var updatedTask = await _taskRepository.GetByIdWithDetailsAsync(taskId);
        return ApiResponse<TaskResponse>.SuccessResponse(MapToTaskResponse(updatedTask!), "Task moved successfully.");
    }

    public async Task<ApiResponse<TaskResponse>> UpdateStatusAsync(Guid userId, Guid taskId, UpdateTaskStatusRequest request)
    {
        var (task, project, isMember, _) = await GetTaskPermissionsAsync(taskId, userId);
        if (task == null || project == null)
        {
            return ApiResponse<TaskResponse>.FailResponse("Task not found.");
        }

        if (!isMember)
        {
            return ApiResponse<TaskResponse>.FailResponse("You do not have access to this project.");
        }

        var targetColumn = await _boardRepository.GetColumnByIdAsync(request.ColumnId);
        if (targetColumn == null)
        {
            return ApiResponse<TaskResponse>.FailResponse("Target column not found.");
        }

        if (targetColumn.Board.ProjectId != project.Id)
        {
            return ApiResponse<TaskResponse>.FailResponse("Cannot move task across different projects.");
        }

        // Check WIP limit if changing columns
        if (task.BoardColumnId != request.ColumnId)
        {
            var currentTasksCount = await _taskRepository.GetColumnTaskCountAsync(request.ColumnId);
            if (targetColumn.WipLimit.HasValue && currentTasksCount >= targetColumn.WipLimit.Value)
            {
                return ApiResponse<TaskResponse>.FailResponse("WIP Limit for the target column has been reached.");
            }

            task.BoardColumnId = request.ColumnId;

            // Handle CompletedAt based on IsDoneColumn
            if (targetColumn.IsDoneColumn)
            {
                task.CompletedAt = DateTime.UtcNow;
            }
            else
            {
                task.CompletedAt = null;
            }

            // Find next position in column
            var tasks = await _boardRepository.GetTasksByColumnIdAsync(request.ColumnId);
            task.Position = tasks.Any() ? tasks.Max(t => t.Position) + 1000 : 1000;
        }

        task.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.SaveChangesAsync();

        try
        {
            await _activityService.LogActivityAsync(userId, project.WorkspaceId, new LogActivityRequest
            {
                EntityType = "Task",
                EntityId = taskId,
                Action = "moved",
                NewValue = targetColumn.Name
            });
        }
        catch { }

        var updatedTask = await _taskRepository.GetByIdWithDetailsAsync(taskId);
        return ApiResponse<TaskResponse>.SuccessResponse(MapToTaskResponse(updatedTask!), "Task status updated successfully.");
    }

    public async Task<ApiResponse<TaskResponse>> UpdatePriorityAsync(Guid userId, Guid taskId, UpdateTaskPriorityRequest request)
    {
        var (task, project, isMember, _) = await GetTaskPermissionsAsync(taskId, userId);
        if (task == null || project == null)
        {
            return ApiResponse<TaskResponse>.FailResponse("Task not found.");
        }

        if (!isMember)
        {
            return ApiResponse<TaskResponse>.FailResponse("You do not have access to this project.");
        }

        task.Priority = request.Priority.Trim().ToLowerInvariant();
        task.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.SaveChangesAsync();

        var updatedTask = await _taskRepository.GetByIdWithDetailsAsync(taskId);
        return ApiResponse<TaskResponse>.SuccessResponse(MapToTaskResponse(updatedTask!), "Task priority updated successfully.");
    }

    public async Task<ApiResponse<TaskResponse>> UpdateDueDateAsync(Guid userId, Guid taskId, UpdateTaskDueDateRequest request)
    {
        var (task, project, isMember, _) = await GetTaskPermissionsAsync(taskId, userId);
        if (task == null || project == null)
        {
            return ApiResponse<TaskResponse>.FailResponse("Task not found.");
        }

        if (!isMember)
        {
            return ApiResponse<TaskResponse>.FailResponse("You do not have access to this project.");
        }

        task.DueDate = request.DueDate;
        task.UpdatedAt = DateTime.UtcNow;

        await _taskRepository.SaveChangesAsync();

        var updatedTask = await _taskRepository.GetByIdWithDetailsAsync(taskId);
        return ApiResponse<TaskResponse>.SuccessResponse(MapToTaskResponse(updatedTask!), "Task due date updated successfully.");
    }

    // Dependencies
    public async Task<ApiResponse<object>> AddDependencyAsync(Guid userId, Guid taskId, AddDependencyRequest request)
    {
        var (task, project, isMember, _) = await GetTaskPermissionsAsync(taskId, userId);
        if (task == null || project == null)
        {
            return ApiResponse<object>.FailResponse("Task not found.");
        }

        if (!isMember)
        {
            return ApiResponse<object>.FailResponse("You do not have access to this project.");
        }

        var dependsOn = await _taskRepository.GetByIdAsync(request.DependsOnId);
        if (dependsOn == null)
        {
            return ApiResponse<object>.FailResponse("Dependent task not found.");
        }

        if (taskId == request.DependsOnId)
        {
            return ApiResponse<object>.FailResponse("A task cannot depend on itself.");
        }

        // Circular check
        var circular = await WouldCreateCircularDependencyAsync(taskId, request.DependsOnId);
        if (circular)
        {
            return ApiResponse<object>.FailResponse("Circular dependency detected.");
        }

        var existing = await _taskRepository.GetDependencyAsync(taskId, request.DependsOnId);
        if (existing != null)
        {
            return ApiResponse<object>.FailResponse("Dependency already exists.");
        }

        var dep = new TaskDependency
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            DependsOnId = request.DependsOnId,
            Type = string.IsNullOrEmpty(request.Type) ? "finish_to_start" : request.Type.Trim().ToLowerInvariant(),
            CreatedAt = DateTime.UtcNow
        };

        await _taskRepository.AddDependencyAsync(dep);
        await _taskRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Dependency added successfully.");
    }

    public async Task<ApiResponse<object>> RemoveDependencyAsync(Guid userId, Guid taskId, Guid dependsOnId)
    {
        var (task, project, isMember, _) = await GetTaskPermissionsAsync(taskId, userId);
        if (task == null || project == null)
        {
            return ApiResponse<object>.FailResponse("Task not found.");
        }

        if (!isMember)
        {
            return ApiResponse<object>.FailResponse("You do not have access to this project.");
        }

        var dep = await _taskRepository.GetDependencyAsync(taskId, dependsOnId);
        if (dep == null)
        {
            return ApiResponse<object>.FailResponse("Dependency not found.");
        }

        _taskRepository.RemoveDependency(dep);
        await _taskRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Dependency removed successfully.");
    }

    // Assignees
    public async Task<ApiResponse<object>> AddAssigneeAsync(Guid userId, Guid taskId, AddAssigneeRequest request)
    {
        var (task, project, isMember, _) = await GetTaskPermissionsAsync(taskId, userId);
        if (task == null || project == null)
        {
            return ApiResponse<object>.FailResponse("Task not found.");
        }

        if (!isMember)
        {
            return ApiResponse<object>.FailResponse("You do not have access to this project.");
        }

        var targetUser = await _userRepository.GetByIdAsync(request.UserId);
        if (targetUser == null || !targetUser.IsActive)
        {
            return ApiResponse<object>.FailResponse("User not found or inactive.");
        }

        var isTargetWorkspaceMember = await _workspaceRepository.IsMemberAsync(project.WorkspaceId, request.UserId);
        if (!isTargetWorkspaceMember)
        {
            return ApiResponse<object>.FailResponse("User must be a member of the workspace before being assigned.");
        }

        var existing = await _taskRepository.GetAssigneeAsync(taskId, request.UserId);
        if (existing != null)
        {
            return ApiResponse<object>.FailResponse("User is already assigned to this task.");
        }

        var assignee = new TaskAssignee
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            UserId = request.UserId,
            CreatedAt = DateTime.UtcNow
        };

        await _taskRepository.AddAssigneeAsync(assignee);
        await _taskRepository.SaveChangesAsync();

        try
        {
            await _activityService.LogActivityAsync(userId, project.WorkspaceId, new LogActivityRequest
            {
                EntityType = "TaskAssignee",
                EntityId = taskId,
                Action = "assigned",
                NewValue = targetUser.DisplayName ?? targetUser.Email
            });

            if (request.UserId != userId)
            {
                await _notificationService.CreateNotificationAsync(
                    request.UserId,
                    project.WorkspaceId,
                    "task_assigned",
                    "Task Assigned",
                    $"You were assigned to task '{task.Title}'.",
                    "Task",
                    taskId);
            }
        }
        catch { }

        return ApiResponse<object>.SuccessResponse(new { }, "Assignee added successfully.");
    }

    public async Task<ApiResponse<object>> RemoveAssigneeAsync(Guid userId, Guid taskId, Guid assigneeUserId)
    {
        var (task, project, isMember, _) = await GetTaskPermissionsAsync(taskId, userId);
        if (task == null || project == null)
        {
            return ApiResponse<object>.FailResponse("Task not found.");
        }

        if (!isMember)
        {
            return ApiResponse<object>.FailResponse("You do not have access to this project.");
        }

        var assignee = await _taskRepository.GetAssigneeAsync(taskId, assigneeUserId);
        if (assignee == null)
        {
            return ApiResponse<object>.FailResponse("Assignee not found for this task.");
        }

        _taskRepository.RemoveAssignee(assignee);
        await _taskRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Assignee removed successfully.");
    }

    // Watchers
    public async Task<ApiResponse<object>> AddWatcherAsync(Guid userId, Guid taskId, AddWatcherRequest request)
    {
        var (task, project, isMember, _) = await GetTaskPermissionsAsync(taskId, userId);
        if (task == null || project == null)
        {
            return ApiResponse<object>.FailResponse("Task not found.");
        }

        if (!isMember)
        {
            return ApiResponse<object>.FailResponse("You do not have access to this project.");
        }

        var targetUser = await _userRepository.GetByIdAsync(request.UserId);
        if (targetUser == null || !targetUser.IsActive)
        {
            return ApiResponse<object>.FailResponse("User not found or inactive.");
        }

        var isTargetWorkspaceMember = await _workspaceRepository.IsMemberAsync(project.WorkspaceId, request.UserId);
        if (!isTargetWorkspaceMember)
        {
            return ApiResponse<object>.FailResponse("User must be a member of the workspace before watching a task.");
        }

        var existing = await _taskRepository.GetWatcherAsync(taskId, request.UserId);
        if (existing != null)
        {
            return ApiResponse<object>.FailResponse("User is already watching this task.");
        }

        var watcher = new TaskWatcher
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            UserId = request.UserId,
            CreatedAt = DateTime.UtcNow
        };

        await _taskRepository.AddWatcherAsync(watcher);
        await _taskRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Watcher added successfully.");
    }

    public async Task<ApiResponse<object>> RemoveWatcherAsync(Guid userId, Guid taskId, Guid watcherUserId)
    {
        var (task, project, isMember, _) = await GetTaskPermissionsAsync(taskId, userId);
        if (task == null || project == null)
        {
            return ApiResponse<object>.FailResponse("Task not found.");
        }

        if (!isMember)
        {
            return ApiResponse<object>.FailResponse("You do not have access to this project.");
        }

        var watcher = await _taskRepository.GetWatcherAsync(taskId, watcherUserId);
        if (watcher == null)
        {
            return ApiResponse<object>.FailResponse("Watcher not found for this task.");
        }

        _taskRepository.RemoveWatcher(watcher);
        await _taskRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Watcher removed successfully.");
    }

    // Circular Dependency DFS Check
    private async Task<bool> WouldCreateCircularDependencyAsync(Guid taskId, Guid dependsOnId)
    {
        if (taskId == dependsOnId) return true;
        var visited = new HashSet<Guid>();
        return await CheckReachabilityAsync(dependsOnId, taskId, visited);
    }

    private async Task<bool> CheckReachabilityAsync(Guid currentId, Guid targetId, HashSet<Guid> visited)
    {
        if (currentId == targetId) return true;
        if (!visited.Add(currentId)) return false;

        var deps = await _taskRepository.GetTaskDependenciesAsync(currentId);
        foreach (var dep in deps)
        {
            if (await CheckReachabilityAsync(dep.DependsOnId, targetId, visited))
            {
                return true;
            }
        }

        return false;
    }

    private static TaskResponse MapToTaskResponse(TaskFlow.Domain.Entities.Task task)
    {
        return new TaskResponse
        {
            Id = task.Id,
            BoardColumnId = task.BoardColumnId,
            ParentTaskId = task.ParentTaskId,
            SprintId = task.SprintId,
            ReporterId = task.ReporterId,
            Title = task.Title,
            Description = task.Description,
            Type = task.Type,
            Priority = task.Priority,
            TaskNumber = task.TaskNumber,
            Position = task.Position,
            StoryPoints = task.StoryPoints,
            StartDate = task.StartDate,
            DueDate = task.DueDate,
            CompletedAt = task.CompletedAt,
            CreatedAt = task.CreatedAt,
            UpdatedAt = task.UpdatedAt,
            Assignees = task.Assignees.Select(ta => new TaskUserDto
            {
                Id = ta.UserId,
                DisplayName = ta.User.DisplayName,
                Email = ta.User.Email,
                AvatarUrl = ta.User.AvatarUrl
            }).ToList(),
            Watchers = task.Watchers.Select(tw => new TaskUserDto
            {
                Id = tw.UserId,
                DisplayName = tw.User.DisplayName,
                Email = tw.User.Email,
                AvatarUrl = tw.User.AvatarUrl
            }).ToList(),
            Dependencies = task.Dependencies.Select(td => new TaskDependencyDto
            {
                DependsOnId = td.DependsOnId,
                Title = td.DependsOn.Title,
                TaskNumber = td.DependsOn.TaskNumber,
                Type = td.Type
            }).ToList(),
            Labels = task.TaskLabels
                .Where(tl => tl.Label != null)
                .Select(tl => new TaskLabelDto
                {
                    Id = tl.LabelId,
                    Name = tl.Label.Name,
                    Color = tl.Label.Color
                }).ToList()
        };
    }
}
