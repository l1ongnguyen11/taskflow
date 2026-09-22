using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Labels;

namespace TaskFlow.Application.Interfaces.Services;

public interface ILabelService
{
    Task<ApiResponse<LabelResponse>> CreateLabelAsync(Guid userId, Guid workspaceId, CreateLabelRequest request);
    Task<ApiResponse<List<LabelResponse>>> GetLabelsByWorkspaceIdAsync(Guid userId, Guid workspaceId);
    Task<ApiResponse<LabelResponse>> GetLabelByIdAsync(Guid userId, Guid labelId);
    Task<ApiResponse<LabelResponse>> UpdateLabelAsync(Guid userId, Guid labelId, UpdateLabelRequest request);
    Task<ApiResponse<object>> DeleteLabelAsync(Guid userId, Guid labelId);

    // Task Labels
    Task<ApiResponse<object>> AddLabelToTaskAsync(Guid userId, Guid taskId, Guid labelId);
    Task<ApiResponse<object>> RemoveLabelFromTaskAsync(Guid userId, Guid taskId, Guid labelId);
}
