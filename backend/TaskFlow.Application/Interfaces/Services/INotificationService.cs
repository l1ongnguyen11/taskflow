using System;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Notifications;

namespace TaskFlow.Application.Interfaces.Services;

public interface INotificationService
{
    System.Threading.Tasks.Task<PaginatedResponse<NotificationResponse>> GetNotificationsAsync(
        Guid userId, bool? isRead, int limit, int offset);
    System.Threading.Tasks.Task<ApiResponse<NotificationResponse>> MarkAsReadAsync(Guid userId, Guid notificationId);
    System.Threading.Tasks.Task<ApiResponse<object>> MarkAllAsReadAsync(Guid userId);
    System.Threading.Tasks.Task<ApiResponse<object>> DeleteAsync(Guid userId, Guid notificationId);
    System.Threading.Tasks.Task<ApiResponse<NotificationResponse>> CreateNotificationAsync(
        Guid userId, Guid? workspaceId, string type, string title, string? body = null, string? entityType = null, Guid? entityId = null);
}
