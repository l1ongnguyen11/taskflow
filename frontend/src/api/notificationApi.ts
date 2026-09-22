import { axiosClient } from './axiosClient';
import { ApiResponse, PaginatedResponse, NotificationItem } from '../types';

export const notificationApi = {
  getNotifications: async (isRead?: boolean, limit = 50, offset = 0) => {
    const params: Record<string, any> = { limit, offset };
    if (isRead !== undefined) {
      params.isRead = isRead;
    }
    const res = await axiosClient.get<PaginatedResponse<NotificationItem>>('/api/notifications', { params });
    return res.data;
  },

  markAsRead: async (notificationId: string) => {
    const res = await axiosClient.patch<ApiResponse<NotificationItem>>(`/api/notifications/${notificationId}/read`);
    return res.data;
  },

  markAllAsRead: async () => {
    const res = await axiosClient.patch<ApiResponse<object>>('/api/notifications/read-all');
    return res.data;
  },

  deleteNotification: async (notificationId: string) => {
    const res = await axiosClient.delete<ApiResponse<object>>(`/api/notifications/${notificationId}`);
    return res.data;
  },
};
