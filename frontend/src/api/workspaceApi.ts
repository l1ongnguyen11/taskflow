import { axiosClient } from './axiosClient';
import { ApiResponse, PaginatedResponse, Workspace, WorkspaceMember, CreateWorkspaceRequest, InviteMemberRequest, WorkspaceInvitation, AcceptInvitationResponse } from '../types';

export const workspaceApi = {
  createWorkspace: async (data: CreateWorkspaceRequest) => {
    const res = await axiosClient.post<ApiResponse<Workspace>>('/api/workspaces', data);
    return res.data;
  },

  getMyWorkspaces: async (limit = 20, offset = 0) => {
    const res = await axiosClient.get<PaginatedResponse<Workspace>>(`/api/workspaces?limit=${limit}&offset=${offset}`);
    return res.data;
  },

  getWorkspace: async (workspaceId: string) => {
    const res = await axiosClient.get<ApiResponse<Workspace>>(`/api/workspaces/${workspaceId}`);
    return res.data;
  },

  updateWorkspace: async (workspaceId: string, data: { name?: string; description?: string; logoUrl?: string }) => {
    const res = await axiosClient.patch<ApiResponse<Workspace>>(`/api/workspaces/${workspaceId}`, data);
    return res.data;
  },

  deleteWorkspace: async (workspaceId: string) => {
    const res = await axiosClient.delete<ApiResponse<object>>(`/api/workspaces/${workspaceId}`);
    return res.data;
  },

  getMembers: async (workspaceId: string, search?: string, limit = 20, offset = 0) => {
    let url = `/api/workspaces/${workspaceId}/members?limit=${limit}&offset=${offset}`;
    if (search && search.trim()) {
      url += `&search=${encodeURIComponent(search.trim())}`;
    }
    const res = await axiosClient.get<PaginatedResponse<WorkspaceMember>>(url);
    return res.data;
  },

  removeMember: async (workspaceId: string, userId: string) => {
    const res = await axiosClient.delete<ApiResponse<object>>(`/api/workspaces/${workspaceId}/members/${userId}`);
    return res.data;
  },

  inviteMember: async (workspaceId: string, data: InviteMemberRequest) => {
    const res = await axiosClient.post<ApiResponse<object>>(`/api/workspaces/${workspaceId}/invitations`, data);
    return res.data;
  },

  changeRole: async (workspaceId: string, userId: string, role: string) => {
    const res = await axiosClient.patch<ApiResponse<object>>(`/api/workspaces/${workspaceId}/members/${userId}/role`, { role });
    return res.data;
  },

  getInvitations: async (workspaceId: string, status = 'pending', limit = 20, offset = 0) => {
    const res = await axiosClient.get<PaginatedResponse<WorkspaceInvitation>>(
      `/api/workspaces/${workspaceId}/invitations?status=${status}&limit=${limit}&offset=${offset}`
    );
    return res.data;
  },

  cancelInvitation: async (workspaceId: string, invitationId: string) => {
    const res = await axiosClient.delete<ApiResponse<object>>(
      `/api/workspaces/${workspaceId}/invitations/${invitationId}`
    );
    return res.data;
  },

  getMyInvitations: async () => {
    const res = await axiosClient.get<ApiResponse<WorkspaceInvitation[]>>('/api/invitations/me');
    return res.data;
  },

  acceptInvitation: async (token: string) => {
    const res = await axiosClient.post<ApiResponse<AcceptInvitationResponse>>(`/api/invitations/${token}/accept`);
    return res.data;
  },

  rejectInvitation: async (token: string) => {
    const res = await axiosClient.post<ApiResponse<object>>(`/api/invitations/${token}/reject`);
    return res.data;
  },
};
