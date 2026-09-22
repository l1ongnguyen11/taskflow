import { axiosClient } from './axiosClient';
import { ApiResponse, FileItem, TaskAttachmentItem } from '../types';

export const fileApi = {
  uploadFile: async (file: File, onProgress?: (percent: number) => void) => {
    const formData = new FormData();
    formData.append('file', file);

    const res = await axiosClient.post<ApiResponse<FileItem>>('/api/files/upload', formData, {
      headers: {
        'Content-Type': 'multipart/form-data',
      },
      onUploadProgress: (progressEvent) => {
        if (progressEvent.total && onProgress) {
          const percent = Math.round((progressEvent.loaded * 100) / progressEvent.total);
          onProgress(percent);
        }
      },
    });
    return res.data;
  },

  downloadFile: async (fileId: string, filename: string) => {
    const res = await axiosClient.get(`/api/files/${fileId}/download`, {
      responseType: 'blob',
    });

    const url = window.URL.createObjectURL(new Blob([res.data]));
    const link = document.createElement('a');
    link.href = url;
    link.setAttribute('download', filename);
    document.body.appendChild(link);
    link.click();
    link.remove();
    window.URL.revokeObjectURL(url);
  },

  deleteFile: async (fileId: string) => {
    const res = await axiosClient.delete<ApiResponse<object>>(`/api/files/${fileId}`);
    return res.data;
  },

  attachToTask: async (taskId: string, fileId: string) => {
    const res = await axiosClient.post<ApiResponse<TaskAttachmentItem>>(`/api/tasks/${taskId}/attachments`, {
      fileId,
    });
    return res.data;
  },

  getTaskAttachments: async (taskId: string) => {
    const res = await axiosClient.get<ApiResponse<TaskAttachmentItem[]>>(`/api/tasks/${taskId}/attachments`);
    return res.data;
  },

  detachFromTask: async (taskId: string, attachmentId: string) => {
    const res = await axiosClient.delete<ApiResponse<object>>(`/api/tasks/${taskId}/attachments/${attachmentId}`);
    return res.data;
  },
};
