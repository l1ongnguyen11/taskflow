import { axiosClient } from './axiosClient';
import {
  ApiResponse,
  Checklist,
  ChecklistItem,
  CreateChecklistRequest,
  UpdateChecklistRequest,
  CreateChecklistItemRequest,
  UpdateChecklistItemRequest,
} from '../types';

export const checklistApi = {
  getTaskChecklists: async (taskId: string) => {
    const res = await axiosClient.get<ApiResponse<Checklist[]>>(`/api/tasks/${taskId}/checklists`);
    return res.data;
  },

  createChecklist: async (taskId: string, title: string) => {
    const res = await axiosClient.post<ApiResponse<Checklist>>(`/api/tasks/${taskId}/checklists`, {
      title,
    } as CreateChecklistRequest);
    return res.data;
  },

  updateChecklist: async (checklistId: string, title: string) => {
    const res = await axiosClient.patch<ApiResponse<Checklist>>(`/api/checklists/${checklistId}`, {
      title,
    } as UpdateChecklistRequest);
    return res.data;
  },

  deleteChecklist: async (checklistId: string) => {
    const res = await axiosClient.delete<ApiResponse<object>>(`/api/checklists/${checklistId}`);
    return res.data;
  },

  createItem: async (checklistId: string, data: CreateChecklistItemRequest) => {
    const res = await axiosClient.post<ApiResponse<ChecklistItem>>(`/api/checklists/${checklistId}/items`, data);
    return res.data;
  },

  updateItem: async (itemId: string, data: UpdateChecklistItemRequest) => {
    const res = await axiosClient.patch<ApiResponse<ChecklistItem>>(`/api/checklist-items/${itemId}`, data);
    return res.data;
  },

  deleteItem: async (itemId: string) => {
    const res = await axiosClient.delete<ApiResponse<object>>(`/api/checklist-items/${itemId}`);
    return res.data;
  },

  toggleItem: async (itemId: string) => {
    const res = await axiosClient.post<ApiResponse<ChecklistItem>>(`/api/checklist-items/${itemId}/toggle`);
    return res.data;
  },
};
