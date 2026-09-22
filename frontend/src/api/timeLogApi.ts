import { axiosClient } from './axiosClient';
import { ApiResponse, TimeLogItem, TimeReportResponse, StartTimerRequest, ManualLogRequest } from '../types';

export const timeLogApi = {
  startTimer: async (taskId: string, description?: string) => {
    const res = await axiosClient.post<ApiResponse<TimeLogItem>>(`/api/tasks/${taskId}/time-logs/start`, {
      description,
    } as StartTimerRequest);
    return res.data;
  },

  stopTimer: async () => {
    const res = await axiosClient.patch<ApiResponse<TimeLogItem>>(`/api/time-logs/stop`);
    return res.data;
  },

  getActiveTimer: async () => {
    const res = await axiosClient.get<ApiResponse<TimeLogItem | null>>(`/api/time-logs/active`);
    return res.data;
  },

  manualLog: async (taskId: string, data: ManualLogRequest) => {
    const res = await axiosClient.post<ApiResponse<TimeLogItem>>(`/api/tasks/${taskId}/time-logs`, data);
    return res.data;
  },

  getTimeReport: async (
    taskId: string,
    params?: { userId?: string; from?: string; to?: string; limit?: number; offset?: number }
  ) => {
    const res = await axiosClient.get<ApiResponse<TimeReportResponse>>(`/api/tasks/${taskId}/time-logs/report`, {
      params,
    });
    return res.data;
  },
};
