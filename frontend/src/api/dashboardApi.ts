import { axiosClient } from './axiosClient';
import {
  ApiResponse,
  WorkspaceStatistics,
  ProjectStatistics,
  TaskStatistics,
  SprintStatistics,
  ActivitySummary,
  TimeTrackingStatistics,
  ProjectReport,
} from '../types';

export const dashboardApi = {
  getWorkspaceStats: async (workspaceId: string) => {
    const res = await axiosClient.get<ApiResponse<WorkspaceStatistics>>(`/api/workspaces/${workspaceId}/dashboard/statistics`);
    return res.data;
  },

  getProjectStats: async (projectId: string) => {
    const res = await axiosClient.get<ApiResponse<ProjectStatistics>>(`/api/projects/${projectId}/dashboard/statistics`);
    return res.data;
  },

  getTaskStats: async (projectId: string) => {
    const res = await axiosClient.get<ApiResponse<TaskStatistics>>(`/api/projects/${projectId}/dashboard/tasks/statistics`);
    return res.data;
  },

  getSprintStats: async (projectId: string) => {
    const res = await axiosClient.get<ApiResponse<SprintStatistics>>(`/api/projects/${projectId}/dashboard/sprints/statistics`);
    return res.data;
  },

  getActivitySummary: async (workspaceId: string) => {
    const res = await axiosClient.get<ApiResponse<ActivitySummary>>(`/api/workspaces/${workspaceId}/dashboard/activities/summary`);
    return res.data;
  },

  getTimeStats: async (projectId: string) => {
    const res = await axiosClient.get<ApiResponse<TimeTrackingStatistics>>(`/api/projects/${projectId}/dashboard/time/statistics`);
    return res.data;
  },

  getProjectReport: async (projectId: string) => {
    const res = await axiosClient.get<ApiResponse<ProjectReport>>(`/api/projects/${projectId}/reports`);
    return res.data;
  },
};
