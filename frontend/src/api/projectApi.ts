import { axiosClient } from './axiosClient';
import { ApiResponse, PaginatedResponse, Project, CreateProjectRequest } from '../types';

export interface ProjectMember {
  userId: string;
  displayName: string;
  email: string;
  avatarUrl?: string | null;
  role: string;
  joinedAt: string;
}

export const projectApi = {
  createProject: async (workspaceId: string, data: CreateProjectRequest) => {
    const res = await axiosClient.post<ApiResponse<Project>>(`/api/workspaces/${workspaceId}/projects`, data);
    return res.data;
  },

  getProjects: async (workspaceId: string, includeArchived = false, limit = 50, offset = 0) => {
    const res = await axiosClient.get<PaginatedResponse<Project>>(
      `/api/workspaces/${workspaceId}/projects?includeArchived=${includeArchived}&limit=${limit}&offset=${offset}`
    );
    return res.data;
  },

  getProject: async (projectId: string) => {
    const res = await axiosClient.get<ApiResponse<Project>>(`/api/projects/${projectId}`);
    return res.data;
  },

  updateProject: async (projectId: string, data: { name?: string; description?: string; leadId?: string }) => {
    const res = await axiosClient.patch<ApiResponse<Project>>(`/api/projects/${projectId}`, data);
    return res.data;
  },

  deleteProject: async (projectId: string) => {
    const res = await axiosClient.delete<ApiResponse<object>>(`/api/projects/${projectId}`);
    return res.data;
  },

  archiveProject: async (projectId: string) => {
    const res = await axiosClient.post<ApiResponse<object>>(`/api/projects/${projectId}/archive`);
    return res.data;
  },

  restoreProject: async (projectId: string) => {
    const res = await axiosClient.post<ApiResponse<object>>(`/api/projects/${projectId}/restore`);
    return res.data;
  },

  getProjectMembers: async (projectId: string, limit = 20, offset = 0) => {
    const res = await axiosClient.get<PaginatedResponse<ProjectMember>>(
      `/api/projects/${projectId}/members?limit=${limit}&offset=${offset}`
    );
    return res.data;
  },

  addProjectMember: async (projectId: string, data: { userId: string; role?: string }) => {
    const res = await axiosClient.post<ApiResponse<ProjectMember>>(`/api/projects/${projectId}/members`, data);
    return res.data;
  },

  removeProjectMember: async (projectId: string, userId: string) => {
    const res = await axiosClient.delete<ApiResponse<object>>(`/api/projects/${projectId}/members/${userId}`);
    return res.data;
  },

  updateProjectMemberRole: async (projectId: string, userId: string, role: string) => {
    const res = await axiosClient.patch<ApiResponse<ProjectMember>>(
      `/api/projects/${projectId}/members/${userId}/role`,
      { role }
    );
    return res.data;
  },
};
