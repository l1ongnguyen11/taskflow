import { axiosClient } from './axiosClient';
import { ApiResponse, UserProfile } from '../types';

export const userApi = {
  getProfile: async () => {
    const res = await axiosClient.get<ApiResponse<UserProfile>>('/api/users/me');
    return res.data;
  },

  updateProfile: async (data: { displayName?: string; avatarUrl?: string }) => {
    const res = await axiosClient.patch<ApiResponse<UserProfile>>('/api/users/me', data);
    return res.data;
  },

  changePassword: async (data: { currentPassword: string; newPassword: string }) => {
    const res = await axiosClient.post<ApiResponse<object>>('/api/users/me/change-password', data);
    return res.data;
  },

  uploadAvatar: async (file: File) => {
    const formData = new FormData();
    formData.append('file', file);
    const res = await axiosClient.post<ApiResponse<UserProfile>>('/api/users/me/avatar', formData, {
      headers: { 'Content-Type': 'multipart/form-data' },
    });
    return res.data;
  },

  getUserById: async (id: string) => {
    const res = await axiosClient.get<ApiResponse<UserProfile>>(`/api/users/${id}`);
    return res.data;
  },
};
