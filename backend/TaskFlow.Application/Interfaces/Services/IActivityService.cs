using System;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Activities;

namespace TaskFlow.Application.Interfaces.Services;

public interface IActivityService
{
    System.Threading.Tasks.Task<ApiResponse<ActivityResponse>> LogActivityAsync(
        Guid userId, Guid workspaceId, LogActivityRequest request);
    System.Threading.Tasks.Task<PaginatedResponse<ActivityResponse>> GetActivityTimelineAsync(
        Guid userId,
        Guid workspaceId,
        string? entityType,
        Guid? actorId,
        Guid? entityId,
        string? action,
        int limit,
        int offset);
}
