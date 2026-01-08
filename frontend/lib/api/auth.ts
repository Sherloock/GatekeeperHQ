import { apiClient } from './client';
import type { LoginRequest, LoginResponse, MeResponse } from '@/types/api';

export const authApi = {
  login: async (data: LoginRequest): Promise<LoginResponse> => {
    const response = await apiClient.axiosInstance.post<LoginResponse>('/auth/login', data);
    if (response.data.token) {
      apiClient.setToken(response.data.token);
    }
    return response.data;
  },

  getMe: async (): Promise<MeResponse> => {
    const response = await apiClient.axiosInstance.get<MeResponse>('/auth/me');
    return response.data;
  },

  logout: () => {
    apiClient.removeToken();
  },
};
