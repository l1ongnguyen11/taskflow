using System;
using System.Threading.Tasks;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Tasks;

namespace TaskFlow.Application.Interfaces.Services;

public interface ITaskService
{
    Task<ApiResponse<TaskResponse>> CreateTaskAsync(Guid userId, Guid columnId, CreateTaskRequest request);
    Task<PaginatedResponse<TaskResponse>> GetBoardTasksAsync(Guid userId, Guid boardId, int limit, int offset);
    Task<ApiResponse<TaskResponse>> GetTaskByIdAsync(Guid userId, Guid taskId);
    Task<ApiResponse<TaskResponse>> UpdateTaskAsync(Guid userId, Guid taskId, UpdateTaskRequest request);
    Task<ApiResponse<object>> DeleteTaskAsync(Guid userId, Guid taskId);
    Task<ApiResponse<TaskResponse>> MoveTaskAsync(Guid userId, Guid taskId, MoveTaskRequest request);
    Task<ApiResponse<TaskResponse>> UpdateStatusAsync(Guid userId, Guid taskId, UpdateTaskStatusRequest request);
    Task<ApiResponse<TaskResponse>> UpdatePriorityAsync(Guid userId, Guid taskId, UpdateTaskPriorityRequest request);
    Task<ApiResponse<TaskResponse>> UpdateDueDateAsync(Guid userId, Guid taskId, UpdateTaskDueDateRequest request);

    // Dependencies
    Task<ApiResponse<object>> AddDependencyAsync(Guid userId, Guid taskId, AddDependencyRequest request);
    Task<ApiResponse<object>> RemoveDependencyAsync(Guid userId, Guid taskId, Guid dependsOnId);

    // Assignees
    Task<ApiResponse<object>> AddAssigneeAsync(Guid userId, Guid taskId, AddAssigneeRequest request);
    Task<ApiResponse<object>> RemoveAssigneeAsync(Guid userId, Guid taskId, Guid assigneeUserId);

    // Watchers
    Task<ApiResponse<object>> AddWatcherAsync(Guid userId, Guid taskId, AddWatcherRequest request);
    Task<ApiResponse<object>> RemoveWatcherAsync(Guid userId, Guid taskId, Guid watcherUserId);
}
