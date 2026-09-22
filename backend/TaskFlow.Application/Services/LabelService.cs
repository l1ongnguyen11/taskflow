using System;
using System.Collections.Generic;
using System.Linq;
using Task = System.Threading.Tasks.Task;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Labels;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Services;

public class LabelService : ILabelService
{
    private readonly ILabelRepository _labelRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IBoardRepository _boardRepository;
    private readonly IProjectRepository _projectRepository;

    public LabelService(
        ILabelRepository labelRepository,
        IWorkspaceRepository workspaceRepository,
        ITaskRepository taskRepository,
        IBoardRepository boardRepository,
        IProjectRepository projectRepository)
    {
        _labelRepository = labelRepository;
        _workspaceRepository = workspaceRepository;
        _taskRepository = taskRepository;
        _boardRepository = boardRepository;
        _projectRepository = projectRepository;
    }

    private async Task<bool> IsWorkspaceOwnerOrAdminAsync(Guid workspaceId, Guid userId)
    {
        var userRoles = await _workspaceRepository.GetUserRolesAsync(workspaceId, userId);
        return userRoles.Any(ur => ur.Role.Name.ToLower() == "owner" || ur.Role.Name.ToLower() == "admin");
    }

    private async Task<(TaskFlow.Domain.Entities.Task? Task, Guid WorkspaceId, bool IsMember)> GetTaskWorkspaceInfoAsync(Guid taskId, Guid userId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null) return (null, Guid.Empty, false);

        var column = await _boardRepository.GetColumnByIdAsync(task.BoardColumnId ?? Guid.Empty);
        if (column == null) return (task, Guid.Empty, false);

        var project = await _projectRepository.GetByIdAsync(column.Board.ProjectId);
        if (project == null || project.DeletedAt != null) return (task, Guid.Empty, false);

        var isMember = await _workspaceRepository.IsMemberAsync(project.WorkspaceId, userId);
        return (task, project.WorkspaceId, isMember);
    }

    public async Task<ApiResponse<LabelResponse>> CreateLabelAsync(Guid userId, Guid workspaceId, CreateLabelRequest request)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            return ApiResponse<LabelResponse>.FailResponse("Workspace not found.");
        }

        var isMember = await _workspaceRepository.IsMemberAsync(workspaceId, userId);
        if (!isMember)
        {
            return ApiResponse<LabelResponse>.FailResponse("You do not have access to this workspace.");
        }

        var isAuthorized = await IsWorkspaceOwnerOrAdminAsync(workspaceId, userId);
        if (!isAuthorized)
        {
            return ApiResponse<LabelResponse>.FailResponse("Only workspace owners or admins can create labels.");
        }

        var name = request.Name.Trim();
        var existingLabel = await _labelRepository.GetByNameAndWorkspaceIdAsync(workspaceId, name);
        if (existingLabel != null)
        {
            return ApiResponse<LabelResponse>.FailResponse("A label with this name already exists in the workspace.");
        }

        var label = new Label
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            Name = name,
            Color = request.Color.Trim(),
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _labelRepository.AddAsync(label);
        await _labelRepository.SaveChangesAsync();

        return ApiResponse<LabelResponse>.SuccessResponse(MapToLabelResponse(label), "Label created successfully.");
    }

    public async Task<ApiResponse<List<LabelResponse>>> GetLabelsByWorkspaceIdAsync(Guid userId, Guid workspaceId)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            return ApiResponse<List<LabelResponse>>.FailResponse("Workspace not found.");
        }

        var isMember = await _workspaceRepository.IsMemberAsync(workspaceId, userId);
        if (!isMember)
        {
            return ApiResponse<List<LabelResponse>>.FailResponse("You do not have access to this workspace.");
        }

        var labels = await _labelRepository.GetByWorkspaceIdAsync(workspaceId);
        var response = labels.Select(MapToLabelResponse).ToList();

        return ApiResponse<List<LabelResponse>>.SuccessResponse(response, "Labels retrieved successfully.");
    }

    public async Task<ApiResponse<LabelResponse>> GetLabelByIdAsync(Guid userId, Guid labelId)
    {
        var label = await _labelRepository.GetByIdAsync(labelId);
        if (label == null)
        {
            return ApiResponse<LabelResponse>.FailResponse("Label not found.");
        }

        var isMember = await _workspaceRepository.IsMemberAsync(label.WorkspaceId, userId);
        if (!isMember)
        {
            return ApiResponse<LabelResponse>.FailResponse("You do not have access to this workspace.");
        }

        return ApiResponse<LabelResponse>.SuccessResponse(MapToLabelResponse(label), "Label retrieved successfully.");
    }

    public async Task<ApiResponse<LabelResponse>> UpdateLabelAsync(Guid userId, Guid labelId, UpdateLabelRequest request)
    {
        var label = await _labelRepository.GetByIdAsync(labelId);
        if (label == null)
        {
            return ApiResponse<LabelResponse>.FailResponse("Label not found.");
        }

        var isAuthorized = await IsWorkspaceOwnerOrAdminAsync(label.WorkspaceId, userId);
        if (!isAuthorized)
        {
            return ApiResponse<LabelResponse>.FailResponse("Only workspace owners or admins can update labels.");
        }

        var name = request.Name.Trim();
        if (!string.Equals(label.Name, name, StringComparison.OrdinalIgnoreCase))
        {
            var existingLabel = await _labelRepository.GetByNameAndWorkspaceIdAsync(label.WorkspaceId, name);
            if (existingLabel != null)
            {
                return ApiResponse<LabelResponse>.FailResponse("A label with this name already exists in the workspace.");
            }
        }

        label.Name = name;
        label.Color = request.Color.Trim();
        label.UpdatedAt = DateTime.UtcNow;

        await _labelRepository.SaveChangesAsync();

        return ApiResponse<LabelResponse>.SuccessResponse(MapToLabelResponse(label), "Label updated successfully.");
    }

    public async Task<ApiResponse<object>> DeleteLabelAsync(Guid userId, Guid labelId)
    {
        var label = await _labelRepository.GetByIdAsync(labelId);
        if (label == null)
        {
            return ApiResponse<object>.FailResponse("Label not found.");
        }

        var isAuthorized = await IsWorkspaceOwnerOrAdminAsync(label.WorkspaceId, userId);
        if (!isAuthorized)
        {
            return ApiResponse<object>.FailResponse("Only workspace owners or admins can delete labels.");
        }

        label.DeletedAt = DateTime.UtcNow;
        label.UpdatedAt = DateTime.UtcNow;

        await _labelRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Label deleted successfully.");
    }

    public async Task<ApiResponse<object>> AddLabelToTaskAsync(Guid userId, Guid taskId, Guid labelId)
    {
        var (task, workspaceId, isMember) = await GetTaskWorkspaceInfoAsync(taskId, userId);
        if (task == null)
        {
            return ApiResponse<object>.FailResponse("Task not found.");
        }

        if (!isMember)
        {
            return ApiResponse<object>.FailResponse("You do not have access to this task.");
        }

        var label = await _labelRepository.GetByIdAsync(labelId);
        if (label == null)
        {
            return ApiResponse<object>.FailResponse("Label not found.");
        }

        if (label.WorkspaceId != workspaceId)
        {
            return ApiResponse<object>.FailResponse("Label must belong to the task's workspace.");
        }

        var existing = await _labelRepository.GetTaskLabelAsync(taskId, labelId);
        if (existing != null)
        {
            return ApiResponse<object>.FailResponse("Label is already added to this task.");
        }

        var taskLabel = new TaskLabel
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            LabelId = labelId,
            CreatedAt = DateTime.UtcNow
        };

        await _labelRepository.AddTaskLabelAsync(taskLabel);
        await _labelRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Label added to task successfully.");
    }

    public async Task<ApiResponse<object>> RemoveLabelFromTaskAsync(Guid userId, Guid taskId, Guid labelId)
    {
        var (task, _, isMember) = await GetTaskWorkspaceInfoAsync(taskId, userId);
        if (task == null)
        {
            return ApiResponse<object>.FailResponse("Task not found.");
        }

        if (!isMember)
        {
            return ApiResponse<object>.FailResponse("You do not have access to this task.");
        }

        var taskLabel = await _labelRepository.GetTaskLabelAsync(taskId, labelId);
        if (taskLabel == null)
        {
            return ApiResponse<object>.FailResponse("Label is not linked to this task.");
        }

        _labelRepository.RemoveTaskLabel(taskLabel);
        await _labelRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Label removed from task successfully.");
    }

    private static LabelResponse MapToLabelResponse(Label label)
    {
        return new LabelResponse
        {
            Id = label.Id,
            WorkspaceId = label.WorkspaceId,
            Name = label.Name,
            Color = label.Color,
            CreatedAt = label.CreatedAt,
            UpdatedAt = label.UpdatedAt
        };
    }
}
