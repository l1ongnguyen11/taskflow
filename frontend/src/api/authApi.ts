import { axiosClient } from './axiosClient';
import { ApiResponse, AuthResponse, UserProfile, ValidateTokenResponse } from '../types';

export const authApi = {
  register: async (data: { email: string; password: string; displayName: string }) => {
    const res = await axiosClient.post<ApiResponse<AuthResponse>>('/api/auth/register', data);
    return res.data;
  },

  login: async (data: { email: string; password: string }) => {
    const res = await axiosClient.post<ApiResponse<AuthResponse>>('/api/auth/login', data);
    return res.data;
  },

  logout: async (refreshToken: string) => {
    const res = await axiosClient.post<ApiResponse<object>>('/api/auth/logout', { refreshToken });
    return res.data;
  },

  validateToken: async () => {
    const res = await axiosClient.get<ApiResponse<ValidateTokenResponse>>('/api/auth/validate');
    return res.data;
  },
};
