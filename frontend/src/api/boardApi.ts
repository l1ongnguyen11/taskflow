import { axiosClient } from './axiosClient';
import { ApiResponse, PaginatedResponse, Board, BoardColumn, CreateBoardRequest, CreateBoardColumnRequest, UpdateBoardColumnRequest } from '../types';

export const boardApi = {
  createBoard: async (projectId: string, data: CreateBoardRequest) => {
    const res = await axiosClient.post<ApiResponse<Board>>(`/api/projects/${projectId}/boards`, data);
    return res.data;
  },

  getBoards: async (projectId: string, limit = 20, offset = 0) => {
    const res = await axiosClient.get<PaginatedResponse<Board>>(`/api/projects/${projectId}/boards?limit=${limit}&offset=${offset}`);
    return res.data;
  },

  getBoard: async (boardId: string) => {
    const res = await axiosClient.get<ApiResponse<Board>>(`/api/boards/${boardId}`);
    return res.data;
  },

  updateBoard: async (boardId: string, data: { name?: string; description?: string }) => {
    const res = await axiosClient.patch<ApiResponse<Board>>(`/api/boards/${boardId}`, data);
    return res.data;
  },

  deleteBoard: async (boardId: string) => {
    const res = await axiosClient.delete<ApiResponse<object>>(`/api/boards/${boardId}`);
    return res.data;
  },

  createColumn: async (boardId: string, data: CreateBoardColumnRequest) => {
    const res = await axiosClient.post<ApiResponse<BoardColumn>>(`/api/boards/${boardId}/columns`, data);
    return res.data;
  },

  updateColumn: async (columnId: string, data: UpdateBoardColumnRequest) => {
    const res = await axiosClient.patch<ApiResponse<BoardColumn>>(`/api/columns/${columnId}`, data);
    return res.data;
  },

  deleteColumn: async (columnId: string) => {
    const res = await axiosClient.delete<ApiResponse<object>>(`/api/columns/${columnId}`);
    return res.data;
  },

  reorderColumns: async (boardId: string, columns: { columnId: string; position: number }[]) => {
    const res = await axiosClient.post<ApiResponse<object>>(`/api/boards/${boardId}/columns/reorder`, { columns });
    return res.data;
  },
};
