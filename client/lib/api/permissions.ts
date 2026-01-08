import { apiClient } from './client';
import type { PermissionDto } from '@/types/api';

export const permissionsApi = {
  getAll: async (): Promise<PermissionDto[]> => {
    const response = await apiClient.axiosInstance.get<PermissionDto[]>('/permissions');
    return response.data;
  },
};
