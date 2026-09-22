using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Checklists;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Services;

public class ChecklistService : IChecklistService
{
    private readonly IChecklistRepository _checklistRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IBoardRepository _boardRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IWorkspaceRepository _workspaceRepository;

    public ChecklistService(
        IChecklistRepository checklistRepository,
        ITaskRepository taskRepository,
        IBoardRepository boardRepository,
        IProjectRepository projectRepository,
        IWorkspaceRepository workspaceRepository)
    {
        _checklistRepository = checklistRepository;
        _taskRepository = taskRepository;
        _boardRepository = boardRepository;
        _projectRepository = projectRepository;
        _workspaceRepository = workspaceRepository;
    }

    private async Task<(bool IsMember, Guid ProjectId)> ValidateTaskAccessAsync(Guid taskId, Guid userId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null) return (false, Guid.Empty);

        var column = await _boardRepository.GetColumnByIdAsync(task.BoardColumnId ?? Guid.Empty);
        if (column == null) return (false, Guid.Empty);

        var project = await _projectRepository.GetByIdAsync(column.Board.ProjectId);
        if (project == null || project.DeletedAt != null) return (false, Guid.Empty);

        var isMember = await _workspaceRepository.IsMemberAsync(project.WorkspaceId, userId);
        return (isMember, project.Id);
    }

    private async Task<(bool IsMember, Guid ProjectId)> ValidateChecklistAccessAsync(Checklist checklist, Guid userId)
    {
        return await ValidateTaskAccessAsync(checklist.TaskId, userId);
    }

    // ========== Checklist CRUD ==========

    public async Task<ApiResponse<ChecklistResponse>> CreateChecklistAsync(Guid userId, Guid taskId, CreateChecklistRequest request)
    {
        var (isMember, _) = await ValidateTaskAccessAsync(taskId, userId);
        if (!isMember)
        {
            return ApiResponse<ChecklistResponse>.FailResponse("Task not found or you do not have access.");
        }

        var existingChecklists = await _checklistRepository.GetByTaskIdAsync(taskId);
        var nextPosition = existingChecklists.Any() ? existingChecklists.Max(c => c.Position) + 1000 : 1000;

        var checklist = new Checklist
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            Title = request.Title.Trim(),
            Position = nextPosition,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _checklistRepository.AddAsync(checklist);
        await _checklistRepository.SaveChangesAsync();

        return ApiResponse<ChecklistResponse>.SuccessResponse(MapToChecklistResponse(checklist), "Checklist created successfully.");
    }

    public async Task<ApiResponse<ChecklistResponse>> GetChecklistByIdAsync(Guid userId, Guid checklistId)
    {
        var checklist = await _checklistRepository.GetByIdWithItemsAsync(checklistId);
        if (checklist == null)
        {
            return ApiResponse<ChecklistResponse>.FailResponse("Checklist not found.");
        }

        var (isMember, _) = await ValidateChecklistAccessAsync(checklist, userId);
        if (!isMember)
        {
            return ApiResponse<ChecklistResponse>.FailResponse("You do not have access to this project.");
        }

        return ApiResponse<ChecklistResponse>.SuccessResponse(MapToChecklistResponse(checklist), "Checklist retrieved successfully.");
    }

    public async Task<ApiResponse<ChecklistResponse>> UpdateChecklistAsync(Guid userId, Guid checklistId, UpdateChecklistRequest request)
    {
        var checklist = await _checklistRepository.GetByIdWithItemsAsync(checklistId);
        if (checklist == null)
        {
            return ApiResponse<ChecklistResponse>.FailResponse("Checklist not found.");
        }

        var (isMember, _) = await ValidateChecklistAccessAsync(checklist, userId);
        if (!isMember)
        {
            return ApiResponse<ChecklistResponse>.FailResponse("You do not have access to this project.");
        }

        checklist.Title = request.Title.Trim();
        checklist.Position = request.Position;
        checklist.UpdatedAt = DateTime.UtcNow;

        await _checklistRepository.SaveChangesAsync();

        return ApiResponse<ChecklistResponse>.SuccessResponse(MapToChecklistResponse(checklist), "Checklist updated successfully.");
    }

    public async Task<ApiResponse<object>> DeleteChecklistAsync(Guid userId, Guid checklistId)
    {
        var checklist = await _checklistRepository.GetByIdWithItemsAsync(checklistId);
        if (checklist == null)
        {
            return ApiResponse<object>.FailResponse("Checklist not found.");
        }

        var (isMember, _) = await ValidateChecklistAccessAsync(checklist, userId);
        if (!isMember)
        {
            return ApiResponse<object>.FailResponse("You do not have access to this project.");
        }

        checklist.DeletedAt = DateTime.UtcNow;
        checklist.UpdatedAt = DateTime.UtcNow;

        // Cascade soft-delete items
        foreach (var item in checklist.Items)
        {
            item.DeletedAt = DateTime.UtcNow;
            item.UpdatedAt = DateTime.UtcNow;
        }

        await _checklistRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Checklist deleted successfully.");
    }

    public async Task<ApiResponse<List<ChecklistResponse>>> GetChecklistsByTaskIdAsync(Guid userId, Guid taskId)
    {
        var (isMember, _) = await ValidateTaskAccessAsync(taskId, userId);
        if (!isMember)
        {
            return ApiResponse<List<ChecklistResponse>>.FailResponse("Task not found or you do not have access.");
        }

        var checklists = await _checklistRepository.GetByTaskIdAsync(taskId);
        var responses = checklists.Select(MapToChecklistResponse).ToList();

        return ApiResponse<List<ChecklistResponse>>.SuccessResponse(responses, "Checklists retrieved successfully.");
    }

    // ========== Checklist Item CRUD ==========

    public async Task<ApiResponse<ChecklistItemResponse>> CreateItemAsync(Guid userId, Guid checklistId, CreateChecklistItemRequest request)
    {
        var checklist = await _checklistRepository.GetByIdWithItemsAsync(checklistId);
        if (checklist == null)
        {
            return ApiResponse<ChecklistItemResponse>.FailResponse("Checklist not found.");
        }

        var (isMember, _) = await ValidateChecklistAccessAsync(checklist, userId);
        if (!isMember)
        {
            return ApiResponse<ChecklistItemResponse>.FailResponse("You do not have access to this project.");
        }

        var nextPosition = checklist.Items.Any() ? checklist.Items.Max(i => i.Position) + 1000 : 1000;

        var item = new ChecklistItem
        {
            Id = Guid.NewGuid(),
            ChecklistId = checklistId,
            Content = request.Content.Trim(),
            IsCompleted = false,
            Position = nextPosition,
            AssigneeId = request.AssigneeId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _checklistRepository.AddItemAsync(item);
        await _checklistRepository.SaveChangesAsync();

        return ApiResponse<ChecklistItemResponse>.SuccessResponse(MapToItemResponse(item), "Checklist item created successfully.");
    }

    public async Task<ApiResponse<ChecklistItemResponse>> UpdateItemAsync(Guid userId, Guid itemId, UpdateChecklistItemRequest request)
    {
        var item = await _checklistRepository.GetItemByIdAsync(itemId);
        if (item == null)
        {
            return ApiResponse<ChecklistItemResponse>.FailResponse("Checklist item not found.");
        }

        var (isMember, _) = await ValidateTaskAccessAsync(item.Checklist.TaskId, userId);
        if (!isMember)
        {
            return ApiResponse<ChecklistItemResponse>.FailResponse("You do not have access to this project.");
        }

        item.Content = request.Content.Trim();
        item.Position = request.Position;
        item.AssigneeId = request.AssigneeId;
        item.UpdatedAt = DateTime.UtcNow;

        await _checklistRepository.SaveChangesAsync();

        return ApiResponse<ChecklistItemResponse>.SuccessResponse(MapToItemResponse(item), "Checklist item updated successfully.");
    }

    public async Task<ApiResponse<object>> DeleteItemAsync(Guid userId, Guid itemId)
    {
        var item = await _checklistRepository.GetItemByIdAsync(itemId);
        if (item == null)
        {
            return ApiResponse<object>.FailResponse("Checklist item not found.");
        }

        var (isMember, _) = await ValidateTaskAccessAsync(item.Checklist.TaskId, userId);
        if (!isMember)
        {
            return ApiResponse<object>.FailResponse("You do not have access to this project.");
        }

        _checklistRepository.RemoveItem(item);
        await _checklistRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Checklist item deleted successfully.");
    }

    // ========== Toggle Completion ==========

    public async Task<ApiResponse<ChecklistItemResponse>> ToggleItemCompletionAsync(Guid userId, Guid itemId)
    {
        var item = await _checklistRepository.GetItemByIdAsync(itemId);
        if (item == null)
        {
            return ApiResponse<ChecklistItemResponse>.FailResponse("Checklist item not found.");
        }

        var (isMember, _) = await ValidateTaskAccessAsync(item.Checklist.TaskId, userId);
        if (!isMember)
        {
            return ApiResponse<ChecklistItemResponse>.FailResponse("You do not have access to this project.");
        }

        item.IsCompleted = !item.IsCompleted;
        item.UpdatedAt = DateTime.UtcNow;

        await _checklistRepository.SaveChangesAsync();

        var message = item.IsCompleted ? "Item marked as completed." : "Item marked as incomplete.";
        return ApiResponse<ChecklistItemResponse>.SuccessResponse(MapToItemResponse(item), message);
    }

    // ========== Mapping ==========

    private static ChecklistResponse MapToChecklistResponse(Checklist checklist)
    {
        return new ChecklistResponse
        {
            Id = checklist.Id,
            TaskId = checklist.TaskId,
            Title = checklist.Title,
            Position = checklist.Position,
            CreatedAt = checklist.CreatedAt,
            UpdatedAt = checklist.UpdatedAt,
            Items = checklist.Items
                .Where(i => i.DeletedAt == null)
                .OrderBy(i => i.Position)
                .Select(MapToItemResponse)
                .ToList()
        };
    }

    private static ChecklistItemResponse MapToItemResponse(ChecklistItem item)
    {
        return new ChecklistItemResponse
        {
            Id = item.Id,
            ChecklistId = item.ChecklistId,
            Content = item.Content,
            IsCompleted = item.IsCompleted,
            Position = item.Position,
            AssigneeId = item.AssigneeId,
            AssigneeName = item.Assignee?.DisplayName,
            CreatedAt = item.CreatedAt,
            UpdatedAt = item.UpdatedAt
        };
    }
}
