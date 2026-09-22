import { axiosClient } from './axiosClient';
import { ApiResponse, LabelItem, CreateLabelRequest, UpdateLabelRequest } from '../types';

export const labelApi = {
  getWorkspaceLabels: async (workspaceId: string) => {
    const res = await axiosClient.get<ApiResponse<LabelItem[]>>(`/api/workspaces/${workspaceId}/labels`);
    return res.data;
  },

  createLabel: async (workspaceId: string, data: CreateLabelRequest) => {
    const res = await axiosClient.post<ApiResponse<LabelItem>>(`/api/workspaces/${workspaceId}/labels`, data);
    return res.data;
  },

  getLabelById: async (labelId: string) => {
    const res = await axiosClient.get<ApiResponse<LabelItem>>(`/api/labels/${labelId}`);
    return res.data;
  },

  updateLabel: async (labelId: string, data: UpdateLabelRequest) => {
    const res = await axiosClient.patch<ApiResponse<LabelItem>>(`/api/labels/${labelId}`, data);
    return res.data;
  },

  deleteLabel: async (labelId: string) => {
    const res = await axiosClient.delete<ApiResponse<object>>(`/api/labels/${labelId}`);
    return res.data;
  },

  addLabelToTask: async (taskId: string, labelId: string) => {
    const res = await axiosClient.post<ApiResponse<object>>(`/api/tasks/${taskId}/labels`, { labelId });
    return res.data;
  },

  removeLabelFromTask: async (taskId: string, labelId: string) => {
    const res = await axiosClient.delete<ApiResponse<object>>(`/api/tasks/${taskId}/labels/${labelId}`);
    return res.data;
  },
};
