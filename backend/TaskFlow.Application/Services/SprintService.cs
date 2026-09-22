using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Activities;
using TaskFlow.Application.DTOs.Sprints;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Services;

public class SprintService : ISprintService
{
    private readonly ISprintRepository _sprintRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IBoardRepository _boardRepository;
    private readonly IActivityService _activityService;
    private readonly INotificationService _notificationService;

    public SprintService(
        ISprintRepository sprintRepository,
        IProjectRepository projectRepository,
        IWorkspaceRepository workspaceRepository,
        ITaskRepository taskRepository,
        IBoardRepository boardRepository,
        IActivityService activityService,
        INotificationService notificationService)
    {
        _sprintRepository = sprintRepository;
        _projectRepository = projectRepository;
        _workspaceRepository = workspaceRepository;
        _taskRepository = taskRepository;
        _boardRepository = boardRepository;
        _activityService = activityService;
        _notificationService = notificationService;
    }

    public async Task<ApiResponse<SprintResponse>> CreateSprintAsync(
        Guid userId, Guid projectId, CreateSprintRequest request)
    {
        var (project, isMember, canManage) = await GetUserProjectPermissionsAsync(projectId, userId);
        if (project == null)
        {
            return ApiResponse<SprintResponse>.FailResponse("Project not found.");
        }

        if (!isMember)
        {
            return ApiResponse<SprintResponse>.FailResponse("You do not have access to this project.");
        }

        if (!canManage)
        {
            return ApiResponse<SprintResponse>.FailResponse("You do not have permission to manage sprints.");
        }

        var sprint = new Sprint
        {
            Id = Guid.NewGuid(),
            ProjectId = projectId,
            Name = request.Name.Trim(),
            Goal = request.Goal?.Trim(),
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            Status = "planning",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _sprintRepository.AddAsync(sprint);
        await _sprintRepository.SaveChangesAsync();

        try
        {
            await _activityService.LogActivityAsync(userId, project.WorkspaceId, new LogActivityRequest
            {
                EntityType = "Sprint",
                EntityId = sprint.Id,
                Action = "created",
                NewValue = sprint.Name
            });
        }
        catch { }

        return ApiResponse<SprintResponse>.SuccessResponse(
            await MapToSprintResponseAsync(sprint), "Sprint created successfully.");
    }

    public async Task<ApiResponse<SprintResponse>> GetSprintByIdAsync(Guid userId, Guid sprintId)
    {
        var sprint = await _sprintRepository.GetByIdAsync(sprintId);
        if (sprint == null)
        {
            return ApiResponse<SprintResponse>.FailResponse("Sprint not found.");
        }

        var (_, isMember, _) = await GetUserProjectPermissionsAsync(sprint.ProjectId, userId);
        if (!isMember)
        {
            return ApiResponse<SprintResponse>.FailResponse("You do not have access to this project.");
        }

        return ApiResponse<SprintResponse>.SuccessResponse(
            await MapToSprintResponseAsync(sprint), "Sprint retrieved successfully.");
    }

    public async Task<PaginatedResponse<SprintResponse>> GetProjectSprintsAsync(
        Guid userId, Guid projectId, int limit, int offset)
    {
        var (project, isMember, _) = await GetUserProjectPermissionsAsync(projectId, userId);
        if (project == null)
        {
            return PaginatedResponse<SprintResponse>.FailResponse("Project not found.");
        }

        if (!isMember)
        {
            return PaginatedResponse<SprintResponse>.FailResponse("You do not have access to this project.");
        }

        var sprints = await _sprintRepository.GetByProjectIdAsync(projectId, limit, offset);
        var totalCount = await _sprintRepository.CountByProjectIdAsync(projectId);
        var responses = new List<SprintResponse>();
        foreach (var sprint in sprints)
        {
            responses.Add(await MapToSprintResponseAsync(sprint));
        }

        return PaginatedResponse<SprintResponse>.SuccessResponse(
            responses, totalCount, limit, offset, "Sprints retrieved successfully.");
    }

    public async Task<ApiResponse<SprintResponse>> UpdateSprintAsync(
        Guid userId, Guid sprintId, UpdateSprintRequest request)
    {
        var sprint = await _sprintRepository.GetByIdAsync(sprintId);
        if (sprint == null)
        {
            return ApiResponse<SprintResponse>.FailResponse("Sprint not found.");
        }

        var (_, isMember, canManage) = await GetUserProjectPermissionsAsync(sprint.ProjectId, userId);
        if (!isMember)
        {
            return ApiResponse<SprintResponse>.FailResponse("You do not have access to this project.");
        }

        if (!canManage)
        {
            return ApiResponse<SprintResponse>.FailResponse("You do not have permission to manage sprints.");
        }

        if (!string.IsNullOrWhiteSpace(request.Name))
        {
            sprint.Name = request.Name.Trim();
        }

        if (request.Goal != null)
        {
            sprint.Goal = string.IsNullOrWhiteSpace(request.Goal) ? null : request.Goal.Trim();
        }

        if (request.StartDate.HasValue)
        {
            sprint.StartDate = request.StartDate;
        }

        if (request.EndDate.HasValue)
        {
            sprint.EndDate = request.EndDate;
        }

        if (sprint.StartDate.HasValue && sprint.EndDate.HasValue && sprint.EndDate < sprint.StartDate)
        {
            return ApiResponse<SprintResponse>.FailResponse("End date must be on or after start date.");
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            var newStatus = request.Status.Trim().ToLowerInvariant();
            if (newStatus == "active")
            {
                var activeError = await EnsureCanActivateSprintAsync(sprint);
                if (activeError != null)
                {
                    return activeError;
                }
            }

            sprint.Status = newStatus;
        }

        sprint.UpdatedAt = DateTime.UtcNow;
        await _sprintRepository.SaveChangesAsync();

        return ApiResponse<SprintResponse>.SuccessResponse(
            await MapToSprintResponseAsync(sprint), "Sprint updated successfully.");
    }

    public async Task<ApiResponse<object>> DeleteSprintAsync(Guid userId, Guid sprintId)
    {
        var sprint = await _sprintRepository.GetByIdAsync(sprintId);
        if (sprint == null)
        {
            return ApiResponse<object>.FailResponse("Sprint not found.");
        }

        var (_, isMember, canManage) = await GetUserProjectPermissionsAsync(sprint.ProjectId, userId);
        if (!isMember)
        {
            return ApiResponse<object>.FailResponse("You do not have access to this project.");
        }

        if (!canManage)
        {
            return ApiResponse<object>.FailResponse("You do not have permission to manage sprints.");
        }

        if (sprint.Status == "active")
        {
            return ApiResponse<object>.FailResponse("Cannot delete an active sprint. Complete or cancel it first.");
        }

        sprint.DeletedAt = DateTime.UtcNow;
        sprint.UpdatedAt = DateTime.UtcNow;
        await _sprintRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Sprint deleted successfully.");
    }

    public async Task<ApiResponse<SprintResponse>> StartSprintAsync(Guid userId, Guid sprintId)
    {
        var sprint = await _sprintRepository.GetByIdAsync(sprintId);
        if (sprint == null)
        {
            return ApiResponse<SprintResponse>.FailResponse("Sprint not found.");
        }

        var (_, isMember, canManage) = await GetUserProjectPermissionsAsync(sprint.ProjectId, userId);
        if (!isMember)
        {
            return ApiResponse<SprintResponse>.FailResponse("You do not have access to this project.");
        }

        if (!canManage)
        {
            return ApiResponse<SprintResponse>.FailResponse("You do not have permission to manage sprints.");
        }

        if (sprint.Status != "planning")
        {
            return ApiResponse<SprintResponse>.FailResponse("Only sprints in planning status can be started.");
        }

        var activeError = await EnsureCanActivateSprintAsync(sprint);
        if (activeError != null)
        {
            return activeError;
        }

        sprint.Status = "active";
        sprint.StartDate ??= DateOnly.FromDateTime(DateTime.UtcNow);
        sprint.UpdatedAt = DateTime.UtcNow;
        await _sprintRepository.SaveChangesAsync();

        try
        {
            var project = await _projectRepository.GetByIdAsync(sprint.ProjectId);
            if (project != null)
            {
                await _activityService.LogActivityAsync(userId, project.WorkspaceId, new LogActivityRequest
                {
                    EntityType = "Sprint",
                    EntityId = sprint.Id,
                    Action = "started",
                    NewValue = sprint.Name
                });

                if (project.LeadId.HasValue && project.LeadId.Value != userId)
                {
                    await _notificationService.CreateNotificationAsync(
                        project.LeadId.Value,
                        project.WorkspaceId,
                        "sprint_started",
                        "Sprint Started",
                        $"Sprint '{sprint.Name}' in project '{project.Name}' was started.",
                        "Sprint",
                        sprint.Id);
                }
            }
        }
        catch { }

        return ApiResponse<SprintResponse>.SuccessResponse(
            await MapToSprintResponseAsync(sprint), "Sprint started successfully.");
    }

    public async Task<ApiResponse<SprintResponse>> CompleteSprintAsync(Guid userId, Guid sprintId)
    {
        var sprint = await _sprintRepository.GetByIdAsync(sprintId);
        if (sprint == null)
        {
            return ApiResponse<SprintResponse>.FailResponse("Sprint not found.");
        }

        var (_, isMember, canManage) = await GetUserProjectPermissionsAsync(sprint.ProjectId, userId);
        if (!isMember)
        {
            return ApiResponse<SprintResponse>.FailResponse("You do not have access to this project.");
        }

        if (!canManage)
        {
            return ApiResponse<SprintResponse>.FailResponse("You do not have permission to manage sprints.");
        }

        if (sprint.Status != "active")
        {
            return ApiResponse<SprintResponse>.FailResponse("Only active sprints can be completed.");
        }

        sprint.Status = "completed";
        sprint.EndDate ??= DateOnly.FromDateTime(DateTime.UtcNow);
        sprint.UpdatedAt = DateTime.UtcNow;
        await _sprintRepository.SaveChangesAsync();

        try
        {
            var project = await _projectRepository.GetByIdAsync(sprint.ProjectId);
            if (project != null)
            {
                await _activityService.LogActivityAsync(userId, project.WorkspaceId, new LogActivityRequest
                {
                    EntityType = "Sprint",
                    EntityId = sprint.Id,
                    Action = "completed",
                    NewValue = sprint.Name
                });
            }
        }
        catch { }

        return ApiResponse<SprintResponse>.SuccessResponse(
            await MapToSprintResponseAsync(sprint), "Sprint completed successfully.");
    }

    public async Task<ApiResponse<SprintResponse>> AddTaskToSprintAsync(
        Guid userId, Guid sprintId, SprintTaskRequest request)
    {
        var sprint = await _sprintRepository.GetByIdAsync(sprintId);
        if (sprint == null)
        {
            return ApiResponse<SprintResponse>.FailResponse("Sprint not found.");
        }

        var (_, isMember, canManage) = await GetUserProjectPermissionsAsync(sprint.ProjectId, userId);
        if (!isMember)
        {
            return ApiResponse<SprintResponse>.FailResponse("You do not have access to this project.");
        }

        if (!canManage)
        {
            return ApiResponse<SprintResponse>.FailResponse("You do not have permission to manage sprints.");
        }

        if (sprint.Status == "completed" || sprint.Status == "cancelled")
        {
            return ApiResponse<SprintResponse>.FailResponse("Cannot add tasks to a completed or cancelled sprint.");
        }

        var task = await _taskRepository.GetByIdAsync(request.TaskId);
        if (task == null)
        {
            return ApiResponse<SprintResponse>.FailResponse("Task not found.");
        }

        var taskProjectId = await GetTaskProjectIdAsync(task);
        if (taskProjectId == null)
        {
            return ApiResponse<SprintResponse>.FailResponse("Task must belong to a board in this project.");
        }

        if (taskProjectId != sprint.ProjectId)
        {
            return ApiResponse<SprintResponse>.FailResponse("Task does not belong to this project.");
        }

        if (task.SprintId == sprintId)
        {
            return ApiResponse<SprintResponse>.FailResponse("Task is already in this sprint.");
        }

        task.SprintId = sprintId;
        task.UpdatedAt = DateTime.UtcNow;
        await _taskRepository.SaveChangesAsync();

        return ApiResponse<SprintResponse>.SuccessResponse(
            await MapToSprintResponseAsync(sprint), "Task added to sprint successfully.");
    }

    public async Task<ApiResponse<SprintResponse>> RemoveTaskFromSprintAsync(
        Guid userId, Guid sprintId, Guid taskId)
    {
        var sprint = await _sprintRepository.GetByIdAsync(sprintId);
        if (sprint == null)
        {
            return ApiResponse<SprintResponse>.FailResponse("Sprint not found.");
        }

        var (_, isMember, canManage) = await GetUserProjectPermissionsAsync(sprint.ProjectId, userId);
        if (!isMember)
        {
            return ApiResponse<SprintResponse>.FailResponse("You do not have access to this project.");
        }

        if (!canManage)
        {
            return ApiResponse<SprintResponse>.FailResponse("You do not have permission to manage sprints.");
        }

        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null)
        {
            return ApiResponse<SprintResponse>.FailResponse("Task not found.");
        }

        if (task.SprintId != sprintId)
        {
            return ApiResponse<SprintResponse>.FailResponse("Task is not assigned to this sprint.");
        }

        task.SprintId = null;
        task.UpdatedAt = DateTime.UtcNow;
        await _taskRepository.SaveChangesAsync();

        return ApiResponse<SprintResponse>.SuccessResponse(
            await MapToSprintResponseAsync(sprint), "Task removed from sprint successfully.");
    }

    private async Task<ApiResponse<SprintResponse>?> EnsureCanActivateSprintAsync(Sprint sprint)
    {
        var activeSprint = await _sprintRepository.GetActiveByProjectIdAsync(sprint.ProjectId);
        if (activeSprint != null && activeSprint.Id != sprint.Id)
        {
            return ApiResponse<SprintResponse>.FailResponse("Another sprint is already active in this project.");
        }

        return null;
    }

    private async Task<Guid?> GetTaskProjectIdAsync(TaskFlow.Domain.Entities.Task task)
    {
        if (!task.BoardColumnId.HasValue)
        {
            return null;
        }

        var column = await _boardRepository.GetColumnByIdAsync(task.BoardColumnId.Value);
        return column?.Board?.ProjectId;
    }

    private async Task<(Project? Project, bool IsMember, bool CanManage)> GetUserProjectPermissionsAsync(
        Guid projectId, Guid userId)
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
        var canManage = userRoles.Any(ur =>
            ur.Role.Name.Equals("owner", StringComparison.OrdinalIgnoreCase) ||
            ur.Role.Name.Equals("admin", StringComparison.OrdinalIgnoreCase)) ||
            project.LeadId == userId;

        return (project, true, canManage);
    }

    private async Task<SprintResponse> MapToSprintResponseAsync(Sprint sprint)
    {
        var taskCount = await _sprintRepository.GetTaskCountAsync(sprint.Id);
        return new SprintResponse
        {
            Id = sprint.Id,
            ProjectId = sprint.ProjectId,
            Name = sprint.Name,
            Goal = sprint.Goal,
            StartDate = sprint.StartDate,
            EndDate = sprint.EndDate,
            Status = sprint.Status,
            TaskCount = taskCount,
            CreatedAt = sprint.CreatedAt,
            UpdatedAt = sprint.UpdatedAt
        };
    }
}
