import axios, { AxiosError, InternalAxiosRequestConfig } from 'axios';

// 1. Centralized Backend Base URL Configuration via environment variables
export const BASE_URL = import.meta.env.VITE_API_BASE_URL || 'http://localhost:5013';

// 2. Centralized Axios HTTP Client Instance
export const axiosClient = axios.create({
  baseURL: BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
  withCredentials: true,
});

// 3. Centralized API Error Message Formatter & Extractor
export const getApiErrorMessage = (error: any): string => {
  if (axios.isAxiosError(error)) {
    const data = error.response?.data;
    if (data) {
      if (Array.isArray(data.errors) && data.errors.length > 0) {
        return data.errors.join(' ');
      }
      if (data.message) {
        return data.message;
      }
    }
    if (error.response?.status === 401) {
      return 'Session expired. Please log in again.';
    }
    if (error.response?.status === 403) {
      return 'You do not have permission to perform this action.';
    }
    if (error.response?.status === 404) {
      return 'Requested resource not found.';
    }
    if (error.message) {
      return error.message;
    }
  }
  return error?.message || 'An unexpected network error occurred.';
};

// 4. Request Interceptor: Automatically Attach JWT Access Token to Authenticated Requests
axiosClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    const token = localStorage.getItem('taskflow_token');
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// 5. Response Interceptor: 401 Handling & Refresh Token Flow
let isRefreshing = false;
let failedQueue: Array<{ resolve: (token: string) => void; reject: (err: any) => void }> = [];

const processQueue = (error: any, token: string | null = null) => {
  failedQueue.forEach((promise) => {
    if (error) {
      promise.reject(error);
    } else {
      promise.resolve(token!);
    }
  });
  failedQueue = [];
};

axiosClient.interceptors.response.use(
  (response) => response,
  async (error: AxiosError) => {
    const originalRequest = error.config as InternalAxiosRequestConfig & { _retry?: boolean };

    if (error.response?.status === 401 && originalRequest && !originalRequest._retry) {
      const requestUrl = originalRequest.url || '';

      // Do not trigger refresh token flow for login, register, or refresh-token requests
      if (
        requestUrl.includes('/api/auth/login') ||
        requestUrl.includes('/api/auth/register') ||
        requestUrl.includes('/api/auth/refresh-token')
      ) {
        return Promise.reject(error);
      }

      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            if (originalRequest.headers) {
              originalRequest.headers.Authorization = `Bearer ${token}`;
            }
            return axiosClient(originalRequest);
          })
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const refreshToken = localStorage.getItem('taskflow_refresh_token');
      if (!refreshToken) {
        localStorage.removeItem('taskflow_token');
        localStorage.removeItem('taskflow_refresh_token');
        if (window.location.pathname !== '/login') {
          window.location.href = '/login';
        }
        return Promise.reject(error);
      }

      try {
        const response = await axios.post(`${BASE_URL}/api/auth/refresh-token`, {
          refreshToken: refreshToken,
        });

        if (response.data?.success && response.data?.data) {
          const newToken = response.data.data.accessToken;
          const newRefreshToken = response.data.data.refreshToken;

          localStorage.setItem('taskflow_token', newToken);
          if (newRefreshToken) {
            localStorage.setItem('taskflow_refresh_token', newRefreshToken);
          }

          axiosClient.defaults.headers.common.Authorization = `Bearer ${newToken}`;
          processQueue(null, newToken);

          if (originalRequest.headers) {
            originalRequest.headers.Authorization = `Bearer ${newToken}`;
          }
          return axiosClient(originalRequest);
        } else {
          throw new Error('Invalid refresh token response');
        }
      } catch (refreshErr) {
        processQueue(refreshErr, null);
        localStorage.removeItem('taskflow_token');
        localStorage.removeItem('taskflow_refresh_token');
        if (window.location.pathname !== '/login') {
          window.location.href = '/login';
        }
        return Promise.reject(refreshErr);
      } finally {
        isRefreshing = false;
      }
    }

    return Promise.reject(error);
  }
);
