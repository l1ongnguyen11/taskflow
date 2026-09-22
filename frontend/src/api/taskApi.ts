import { axiosClient } from './axiosClient';
import { ApiResponse, PaginatedResponse, TaskItem, CreateTaskRequest, UpdateTaskRequest } from '../types';

export const taskApi = {
  createTask: async (columnId: string, data: CreateTaskRequest) => {
    const res = await axiosClient.post<ApiResponse<TaskItem>>(`/api/columns/${columnId}/tasks`, data);
    return res.data;
  },

  getBoardTasks: async (boardId: string) => {
    const res = await axiosClient.get<PaginatedResponse<TaskItem>>(`/api/boards/${boardId}/tasks`);
    return res.data;
  },

  getTask: async (taskId: string) => {
    const res = await axiosClient.get<ApiResponse<TaskItem>>(`/api/tasks/${taskId}`);
    return res.data;
  },

  updateTask: async (taskId: string, data: UpdateTaskRequest) => {
    const res = await axiosClient.patch<ApiResponse<TaskItem>>(`/api/tasks/${taskId}`, data);
    return res.data;
  },

  deleteTask: async (taskId: string) => {
    const res = await axiosClient.delete<ApiResponse<object>>(`/api/tasks/${taskId}`);
    return res.data;
  },

  addAssignee: async (taskId: string, userId: string) => {
    const res = await axiosClient.post<ApiResponse<object>>(`/api/tasks/${taskId}/assignees`, { userId });
    return res.data;
  },

  removeAssignee: async (taskId: string, userId: string) => {
    const res = await axiosClient.delete<ApiResponse<object>>(`/api/tasks/${taskId}/assignees/${userId}`);
    return res.data;
  },

  addWatcher: async (taskId: string, userId: string) => {
    const res = await axiosClient.post<ApiResponse<object>>(`/api/tasks/${taskId}/watchers`, { userId });
    return res.data;
  },

  removeWatcher: async (taskId: string, userId: string) => {
    const res = await axiosClient.delete<ApiResponse<object>>(`/api/tasks/${taskId}/watchers/${userId}`);
    return res.data;
  },

  moveTask: async (taskId: string, columnId: string, newPosition: number) => {
    const res = await axiosClient.post<ApiResponse<TaskItem>>(`/api/tasks/${taskId}/move`, {
      targetColumnId: columnId,
      position: newPosition,
    });
    return res.data;
  },

  addDependency: async (taskId: string, dependsOnId: string, type: string = 'finish_to_start') => {
    const res = await axiosClient.post<ApiResponse<object>>(`/api/tasks/${taskId}/dependencies`, {
      dependsOnId,
      type,
    });
    return res.data;
  },

  removeDependency: async (taskId: string, dependsOnId: string) => {
    const res = await axiosClient.delete<ApiResponse<object>>(`/api/tasks/${taskId}/dependencies/${dependsOnId}`);
    return res.data;
  },
};
