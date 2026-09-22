using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Checklists;

namespace TaskFlow.Application.Interfaces.Services;

public interface IChecklistService
{
    // Checklist CRUD
    Task<ApiResponse<ChecklistResponse>> CreateChecklistAsync(Guid userId, Guid taskId, CreateChecklistRequest request);
    Task<ApiResponse<ChecklistResponse>> GetChecklistByIdAsync(Guid userId, Guid checklistId);
    Task<ApiResponse<ChecklistResponse>> UpdateChecklistAsync(Guid userId, Guid checklistId, UpdateChecklistRequest request);
    Task<ApiResponse<object>> DeleteChecklistAsync(Guid userId, Guid checklistId);
    Task<ApiResponse<List<ChecklistResponse>>> GetChecklistsByTaskIdAsync(Guid userId, Guid taskId);

    // Checklist Item CRUD
    Task<ApiResponse<ChecklistItemResponse>> CreateItemAsync(Guid userId, Guid checklistId, CreateChecklistItemRequest request);
    Task<ApiResponse<ChecklistItemResponse>> UpdateItemAsync(Guid userId, Guid itemId, UpdateChecklistItemRequest request);
    Task<ApiResponse<object>> DeleteItemAsync(Guid userId, Guid itemId);

    // Complete / Uncomplete
    Task<ApiResponse<ChecklistItemResponse>> ToggleItemCompletionAsync(Guid userId, Guid itemId);
}
