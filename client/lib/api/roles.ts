import { apiClient } from './client';
import type { RoleDto, CreateRoleRequest, UpdateRoleRequest, PermissionDto, AddPermissionRequest } from '@/types/api';

export const rolesApi = {
  getAll: async (): Promise<RoleDto[]> => {
    const response = await apiClient.axiosInstance.get<RoleDto[]>('/roles');
    return response.data;
  },

  getById: async (id: number): Promise<RoleDto> => {
    const response = await apiClient.axiosInstance.get<RoleDto>(`/roles/${id}`);
    return response.data;
  },

  create: async (data: CreateRoleRequest): Promise<RoleDto> => {
    const response = await apiClient.axiosInstance.post<RoleDto>('/roles', data);
    return response.data;
  },

  update: async (id: number, data: UpdateRoleRequest): Promise<RoleDto> => {
    const response = await apiClient.axiosInstance.put<RoleDto>(`/roles/${id}`, data);
    return response.data;
  },

  delete: async (id: number): Promise<void> => {
    await apiClient.axiosInstance.delete(`/roles/${id}`);
  },

  getPermissions: async (id: number): Promise<PermissionDto[]> => {
    const response = await apiClient.axiosInstance.get<PermissionDto[]>(`/roles/${id}/permissions`);
    return response.data;
  },

  addPermission: async (id: number, data: AddPermissionRequest): Promise<void> => {
    await apiClient.axiosInstance.post(`/roles/${id}/permissions`, data);
  },

  removePermission: async (id: number, permissionId: number): Promise<void> => {
    await apiClient.axiosInstance.delete(`/roles/${id}/permissions/${permissionId}`);
  },
};
