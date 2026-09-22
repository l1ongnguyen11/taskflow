import { axiosClient } from './axiosClient';
import { ApiResponse, PaginatedResponse, ActivityItem } from '../types';

export interface GetActivitiesParams {
  entityType?: string;
  actorId?: string;
  entityId?: string;
  action?: string;
  limit?: number;
  offset?: number;
}

export const activityApi = {
  getActivities: async (workspaceId: string, params?: GetActivitiesParams) => {
    const queryParams: Record<string, any> = {};
    if (params?.entityType) queryParams.entityType = params.entityType;
    if (params?.actorId) queryParams.actorId = params.actorId;
    if (params?.entityId) queryParams.entityId = params.entityId;
    if (params?.action) queryParams.action = params.action;
    if (params?.limit !== undefined) queryParams.limit = params.limit;
    if (params?.offset !== undefined) queryParams.offset = params.offset;

    const res = await axiosClient.get<PaginatedResponse<ActivityItem>>(
      `/api/workspaces/${workspaceId}/activities`,
      { params: queryParams }
    );
    return res.data;
  },

  logActivity: async (
    workspaceId: string,
    entityType: string,
    entityId: string,
    action: string,
    oldValue?: string,
    newValue?: string
  ) => {
    const res = await axiosClient.post<ApiResponse<ActivityItem>>(
      `/api/workspaces/${workspaceId}/activities`,
      {
        entityType,
        entityId,
        action,
        oldValue,
        newValue,
      }
    );
    return res.data;
  },
};
