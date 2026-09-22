using System;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Sprints;

namespace TaskFlow.Application.Interfaces.Services;

public interface ISprintService
{
    System.Threading.Tasks.Task<ApiResponse<SprintResponse>> CreateSprintAsync(
        Guid userId, Guid projectId, CreateSprintRequest request);
    System.Threading.Tasks.Task<ApiResponse<SprintResponse>> GetSprintByIdAsync(Guid userId, Guid sprintId);
    System.Threading.Tasks.Task<PaginatedResponse<SprintResponse>> GetProjectSprintsAsync(
        Guid userId, Guid projectId, int limit, int offset);
    System.Threading.Tasks.Task<ApiResponse<SprintResponse>> UpdateSprintAsync(
        Guid userId, Guid sprintId, UpdateSprintRequest request);
    System.Threading.Tasks.Task<ApiResponse<object>> DeleteSprintAsync(Guid userId, Guid sprintId);
    System.Threading.Tasks.Task<ApiResponse<SprintResponse>> StartSprintAsync(Guid userId, Guid sprintId);
    System.Threading.Tasks.Task<ApiResponse<SprintResponse>> CompleteSprintAsync(Guid userId, Guid sprintId);
    System.Threading.Tasks.Task<ApiResponse<SprintResponse>> AddTaskToSprintAsync(
        Guid userId, Guid sprintId, SprintTaskRequest request);
    System.Threading.Tasks.Task<ApiResponse<SprintResponse>> RemoveTaskFromSprintAsync(
        Guid userId, Guid sprintId, Guid taskId);
}
