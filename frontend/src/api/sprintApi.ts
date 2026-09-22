import { axiosClient } from './axiosClient';
import { ApiResponse, PaginatedResponse, Sprint, CreateSprintRequest, UpdateSprintRequest } from '../types';

export const sprintApi = {
  getProjectSprints: async (projectId: string, limit: number = 50, offset: number = 0) => {
    const res = await axiosClient.get<PaginatedResponse<Sprint>>(`/api/projects/${projectId}/sprints`, {
      params: { limit, offset },
    });
    return res.data;
  },

  getSprintById: async (sprintId: string) => {
    const res = await axiosClient.get<ApiResponse<Sprint>>(`/api/sprints/${sprintId}`);
    return res.data;
  },

  createSprint: async (projectId: string, data: CreateSprintRequest) => {
    const res = await axiosClient.post<ApiResponse<Sprint>>(`/api/projects/${projectId}/sprints`, data);
    return res.data;
  },

  updateSprint: async (sprintId: string, data: UpdateSprintRequest) => {
    const res = await axiosClient.patch<ApiResponse<Sprint>>(`/api/sprints/${sprintId}`, data);
    return res.data;
  },

  deleteSprint: async (sprintId: string) => {
    const res = await axiosClient.delete<ApiResponse<object>>(`/api/sprints/${sprintId}`);
    return res.data;
  },

  startSprint: async (sprintId: string) => {
    const res = await axiosClient.patch<ApiResponse<Sprint>>(`/api/sprints/${sprintId}/start`);
    return res.data;
  },

  completeSprint: async (sprintId: string) => {
    const res = await axiosClient.patch<ApiResponse<Sprint>>(`/api/sprints/${sprintId}/complete`);
    return res.data;
  },

  addTaskToSprint: async (sprintId: string, taskId: string) => {
    const res = await axiosClient.post<ApiResponse<Sprint>>(`/api/sprints/${sprintId}/tasks`, { taskId });
    return res.data;
  },

  removeTaskFromSprint: async (sprintId: string, taskId: string) => {
    const res = await axiosClient.delete<ApiResponse<Sprint>>(`/api/sprints/${sprintId}/tasks/${taskId}`);
    return res.data;
  },
};
