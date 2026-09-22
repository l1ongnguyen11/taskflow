using System;
using System.Collections.Generic;
using System.Linq;
using Task = System.Threading.Tasks.Task;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Activities;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Services;

public class ActivityService : IActivityService
{
    private readonly IActivityRepository _activityRepository;
    private readonly IWorkspaceRepository _workspaceRepository;

    public ActivityService(
        IActivityRepository activityRepository,
        IWorkspaceRepository workspaceRepository)
    {
        _activityRepository = activityRepository;
        _workspaceRepository = workspaceRepository;
    }

    public async Task<ApiResponse<ActivityResponse>> LogActivityAsync(
        Guid userId, Guid workspaceId, LogActivityRequest request)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            return ApiResponse<ActivityResponse>.FailResponse("Workspace not found.");
        }

        var isMember = await _workspaceRepository.IsMemberAsync(workspaceId, userId);
        if (!isMember)
        {
            return ApiResponse<ActivityResponse>.FailResponse("You do not have access to this workspace.");
        }

        var activity = new Activity
        {
            Id = Guid.NewGuid(),
            WorkspaceId = workspaceId,
            ActorId = userId,
            EntityType = request.EntityType.Trim(),
            EntityId = request.EntityId,
            Action = request.Action.Trim().ToLowerInvariant(),
            OldValue = request.OldValue,
            NewValue = request.NewValue,
            CreatedAt = DateTime.UtcNow
        };

        await _activityRepository.AddAsync(activity);
        await _activityRepository.SaveChangesAsync();

        var reloaded = await _activityRepository.GetByIdAsync(activity.Id);
        return ApiResponse<ActivityResponse>.SuccessResponse(
            MapToActivityResponse(reloaded!), "Activity logged successfully.");
    }

    public async Task<PaginatedResponse<ActivityResponse>> GetActivityTimelineAsync(
        Guid userId,
        Guid workspaceId,
        string? entityType,
        Guid? actorId,
        Guid? entityId,
        string? action,
        int limit,
        int offset)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            return PaginatedResponse<ActivityResponse>.FailResponse("Workspace not found.");
        }

        var isMember = await _workspaceRepository.IsMemberAsync(workspaceId, userId);
        if (!isMember)
        {
            return PaginatedResponse<ActivityResponse>.FailResponse("You do not have access to this workspace.");
        }

        var activities = await _activityRepository.GetByWorkspaceIdAsync(
            workspaceId, entityType, actorId, entityId, action, limit, offset);
        var totalCount = await _activityRepository.CountByWorkspaceIdAsync(
            workspaceId, entityType, actorId, entityId, action);
        var responses = activities.Select(MapToActivityResponse).ToList();

        return PaginatedResponse<ActivityResponse>.SuccessResponse(
            responses, totalCount, limit, offset, "Activity timeline retrieved successfully.");
    }

    private static ActivityResponse MapToActivityResponse(Activity activity)
    {
        return new ActivityResponse
        {
            Id = activity.Id,
            WorkspaceId = activity.WorkspaceId,
            ActorId = activity.ActorId,
            ActorDisplayName = activity.Actor?.DisplayName,
            ActorAvatarUrl = activity.Actor?.AvatarUrl,
            EntityType = activity.EntityType,
            EntityId = activity.EntityId,
            Action = activity.Action,
            OldValue = activity.OldValue,
            NewValue = activity.NewValue,
            CreatedAt = activity.CreatedAt
        };
    }
}
