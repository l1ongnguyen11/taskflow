using System;
using System.Collections.Generic;
using System.Linq;
using Task = System.Threading.Tasks.Task;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Notifications;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Services;

public class NotificationService : INotificationService
{
    private readonly INotificationRepository _notificationRepository;

    public NotificationService(INotificationRepository notificationRepository)
    {
        _notificationRepository = notificationRepository;
    }

    public async Task<PaginatedResponse<NotificationResponse>> GetNotificationsAsync(
        Guid userId, bool? isRead, int limit, int offset)
    {
        var notifications = await _notificationRepository.GetByUserIdAsync(userId, isRead, limit, offset);
        var totalCount = await _notificationRepository.CountByUserIdAsync(userId, isRead);
        var responses = notifications.Select(MapToNotificationResponse).ToList();

        return PaginatedResponse<NotificationResponse>.SuccessResponse(
            responses, totalCount, limit, offset, "Notifications retrieved successfully.");
    }

    public async Task<ApiResponse<NotificationResponse>> MarkAsReadAsync(Guid userId, Guid notificationId)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId);
        if (notification == null)
        {
            return ApiResponse<NotificationResponse>.FailResponse("Notification not found.");
        }

        if (notification.UserId != userId)
        {
            return ApiResponse<NotificationResponse>.FailResponse("You do not have access to this notification.");
        }

        if (!notification.IsRead)
        {
            notification.IsRead = true;
            notification.UpdatedAt = DateTime.UtcNow;
            await _notificationRepository.SaveChangesAsync();
        }

        return ApiResponse<NotificationResponse>.SuccessResponse(
            MapToNotificationResponse(notification), "Notification marked as read.");
    }

    public async Task<ApiResponse<object>> MarkAllAsReadAsync(Guid userId)
    {
        var updatedCount = await _notificationRepository.MarkAllAsReadAsync(userId);
        await _notificationRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(
            new { updated_count = updatedCount },
            updatedCount > 0
                ? $"{updatedCount} notification(s) marked as read."
                : "No unread notifications to update.");
    }

    public async Task<ApiResponse<object>> DeleteAsync(Guid userId, Guid notificationId)
    {
        var notification = await _notificationRepository.GetByIdAsync(notificationId);
        if (notification == null)
        {
            return ApiResponse<object>.FailResponse("Notification not found.");
        }

        if (notification.UserId != userId)
        {
            return ApiResponse<object>.FailResponse("You do not have access to this notification.");
        }

        notification.DeletedAt = DateTime.UtcNow;
        notification.UpdatedAt = DateTime.UtcNow;
        await _notificationRepository.SaveChangesAsync();

        return ApiResponse<object>.SuccessResponse(new { }, "Notification deleted successfully.");
    }

    public async Task<ApiResponse<NotificationResponse>> CreateNotificationAsync(
        Guid userId, Guid? workspaceId, string type, string title, string? body = null, string? entityType = null, Guid? entityId = null)
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            WorkspaceId = workspaceId,
            Type = type,
            Title = title,
            Body = body,
            EntityType = entityType,
            EntityId = entityId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _notificationRepository.AddAsync(notification);
        await _notificationRepository.SaveChangesAsync();

        return ApiResponse<NotificationResponse>.SuccessResponse(MapToNotificationResponse(notification), "Notification created.");
    }

    private static NotificationResponse MapToNotificationResponse(Notification notification)
    {
        return new NotificationResponse
        {
            Id = notification.Id,
            UserId = notification.UserId,
            WorkspaceId = notification.WorkspaceId,
            Type = notification.Type,
            Title = notification.Title,
            Body = notification.Body,
            EntityType = notification.EntityType,
            EntityId = notification.EntityId,
            IsRead = notification.IsRead,
            CreatedAt = notification.CreatedAt,
            UpdatedAt = notification.UpdatedAt
        };
    }
}
