import { axiosClient } from './axiosClient';
import { ApiResponse, Comment } from '../types';

export const commentApi = {
  getTaskComments: async (taskId: string) => {
    const res = await axiosClient.get<ApiResponse<Comment[]>>(`/api/tasks/${taskId}/comments`);
    return res.data;
  },

  createComment: async (taskId: string, body: string, parentCommentId?: string) => {
    const res = await axiosClient.post<ApiResponse<Comment>>(`/api/tasks/${taskId}/comments`, {
      body,
      parentCommentId,
    });
    return res.data;
  },

  updateComment: async (commentId: string, body: string) => {
    const res = await axiosClient.patch<ApiResponse<Comment>>(`/api/comments/${commentId}`, {
      body,
    });
    return res.data;
  },

  deleteComment: async (commentId: string) => {
    const res = await axiosClient.delete<ApiResponse<object>>(`/api/comments/${commentId}`);
    return res.data;
  },
};

