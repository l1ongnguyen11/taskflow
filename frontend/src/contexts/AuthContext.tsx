import React, { createContext, useContext, useState, useEffect } from 'react';
import { UserProfile } from '../types';
import { authApi } from '../api/authApi';
import { userApi } from '../api/userApi';

interface AuthContextType {
  user: UserProfile | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (token: string, refreshToken: string) => Promise<void>;
  logout: () => Promise<void>;
  updateUser: (user: UserProfile) => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const AuthProvider: React.FC<{ children: React.ReactNode }> = ({ children }) => {
  const [user, setUser] = useState<UserProfile | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);

  const fetchProfile = async () => {
    try {
      const res = await userApi.getProfile();
      if (res.success && res.data) {
        setUser(res.data);
      } else {
        setUser(null);
      }
    } catch {
      setUser(null);
    } finally {
      setIsLoading(false);
    }
  };

  useEffect(() => {
    const token = localStorage.getItem('taskflow_token');
    if (token) {
      fetchProfile();
    } else {
      setIsLoading(false);
    }
  }, []);

  const login = async (token: string, refreshToken: string) => {
    localStorage.setItem('taskflow_token', token);
    localStorage.setItem('taskflow_refresh_token', refreshToken);
    setIsLoading(true);
    await fetchProfile();
  };

  const logout = async () => {
    const refreshToken = localStorage.getItem('taskflow_refresh_token') || '';
    try {
      if (refreshToken) {
        await authApi.logout(refreshToken);
      }
    } catch (e) {
      console.error('Logout failed on backend:', e);
    } finally {
      localStorage.removeItem('taskflow_token');
      localStorage.removeItem('taskflow_refresh_token');
      setUser(null);
    }
  };

  const updateUser = (updatedUser: UserProfile) => {
    setUser(updatedUser);
  };

  return (
    <AuthContext.Provider
      value={{
        user,
        isAuthenticated: !!user,
        isLoading,
        login,
        logout,
        updateUser,
      }}
    >
      {children}
    </AuthContext.Provider>
  );
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
