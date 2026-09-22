import React, { createContext, useContext, useState, useEffect } from 'react';
import { Workspace, Project, CreateWorkspaceRequest } from '../types';
import { workspaceApi } from '../api/workspaceApi';
import { projectApi } from '../api/projectApi';
import { useAuth } from './AuthContext';

interface WorkspaceContextType {
  workspaces: Workspace[];
  currentWorkspace: Workspace | null;
  projects: Project[];
  isLoadingWorkspaces: boolean;
  workspaceError: string | null;
  selectWorkspace: (workspaceId: string) => void;
  refreshWorkspaces: () => Promise<void>;
  refreshProjects: () => Promise<void>;
  clearWorkspaceError: () => void;
  createWorkspace: (data: CreateWorkspaceRequest) => Promise<Workspace>;
  updateWorkspace: (workspaceId: string, data: { name: string; description?: string; logoUrl?: string }) => Promise<Workspace>;
}

const WorkspaceContext = createContext<WorkspaceContextType | undefined>(undefined);

export const WorkspaceProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated } = useAuth();
  const [workspaces, setWorkspaces] = useState<Workspace[]>([]);
  const [currentWorkspace, setCurrentWorkspace] = useState<Workspace | null>(null);
  const [projects, setProjects] = useState<Project[]>([]);
  const [isLoadingWorkspaces, setIsLoadingWorkspaces] = useState<boolean>(false);
  const [workspaceError, setWorkspaceError] = useState<string | null>(null);

  const fetchWorkspaces = async () => {
    if (!isAuthenticated) return;
    setIsLoadingWorkspaces(true);
    setWorkspaceError(null);
    try {
      const res = await workspaceApi.getMyWorkspaces();
      if (res.success && res.data) {
        setWorkspaces(res.data);
        const savedWsId = localStorage.getItem('taskflow_current_ws');
        const found = res.data.find((w) => w.id === savedWsId) || res.data[0] || null;
        setCurrentWorkspace(found);
      }
    } catch (e: any) {
      const msg = e.response?.data?.message || e.message || 'Failed to load workspaces.';
      setWorkspaceError(msg);
      console.error('Failed to load workspaces:', e);
    } finally {
      setIsLoadingWorkspaces(false);
    }
  };

  const fetchProjects = async () => {
    if (!currentWorkspace) {
      setProjects([]);
      return;
    }
    try {
      const res = await projectApi.getProjects(currentWorkspace.id);
      if (res.success && res.data) {
        setProjects(res.data);
      }
    } catch (e) {
      console.error('Failed to load projects:', e);
    }
  };

  useEffect(() => {
    if (isAuthenticated) {
      fetchWorkspaces();
    } else {
      setWorkspaces([]);
      setCurrentWorkspace(null);
      setProjects([]);
      setWorkspaceError(null);
    }
  }, [isAuthenticated]);

  useEffect(() => {
    if (currentWorkspace) {
      localStorage.setItem('taskflow_current_ws', currentWorkspace.id);
      fetchProjects();
    }
  }, [currentWorkspace]);

  const selectWorkspace = (workspaceId: string) => {
    const found = workspaces.find((w) => w.id === workspaceId);
    if (found) {
      setCurrentWorkspace(found);
    }
  };

  const clearWorkspaceError = () => {
    setWorkspaceError(null);
  };

  const createWorkspace = async (data: CreateWorkspaceRequest): Promise<Workspace> => {
    const res = await workspaceApi.createWorkspace(data);
    if (res.success && res.data) {
      const newWs = res.data;
      await fetchWorkspaces();
      setCurrentWorkspace(newWs);
      localStorage.setItem('taskflow_current_ws', newWs.id);
      return newWs;
    } else {
      throw new Error(res.message || 'Failed to create workspace');
    }
  };

  const updateWorkspace = async (
    workspaceId: string,
    data: { name: string; description?: string; logoUrl?: string }
  ): Promise<Workspace> => {
    const res = await workspaceApi.updateWorkspace(workspaceId, data);
    if (res.success && res.data) {
      const updatedWs = res.data;
      setWorkspaces((prev) => prev.map((w) => (w.id === workspaceId ? { ...w, ...updatedWs } : w)));
      if (currentWorkspace?.id === workspaceId) {
        setCurrentWorkspace((prev) => (prev ? { ...prev, ...updatedWs } : updatedWs));
      }
      return updatedWs;
    } else {
      throw new Error(res.message || 'Failed to update workspace');
    }
  };

  return (
    <WorkspaceContext.Provider
      value={{
        workspaces,
        currentWorkspace,
        projects,
        isLoadingWorkspaces,
        workspaceError,
        selectWorkspace,
        refreshWorkspaces: fetchWorkspaces,
        refreshProjects: fetchProjects,
        clearWorkspaceError,
        createWorkspace,
        updateWorkspace,
      }}
    >
      {children}
    </WorkspaceContext.Provider>
  );
};

export const useWorkspace = () => {
  const context = useContext(WorkspaceContext);
  if (!context) {
    throw new Error('useWorkspace must be used within a WorkspaceProvider');
  }
  return context;
};
