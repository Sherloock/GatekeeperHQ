import type { CreateTenantRequest, TenantDto, UpdateTenantRequest } from '@/types/api';
import { apiClient } from './client';

export const tenantsApi = {
	getAll: async (): Promise<TenantDto[]> => {
		const response = await apiClient.axiosInstance.get<TenantDto[]>('/tenants');
		return response.data;
	},

	getById: async (id: number): Promise<TenantDto> => {
		const response = await apiClient.axiosInstance.get<TenantDto>(`/tenants/${id}`);
		return response.data;
	},

	create: async (data: CreateTenantRequest): Promise<TenantDto> => {
		const response = await apiClient.axiosInstance.post<TenantDto>('/tenants', data);
		return response.data;
	},

	update: async (id: number, data: UpdateTenantRequest): Promise<TenantDto> => {
		const response = await apiClient.axiosInstance.put<TenantDto>(`/tenants/${id}`, data);
		return response.data;
	},

	delete: async (id: number): Promise<void> => {
		await apiClient.axiosInstance.delete(`/tenants/${id}`);
	},

	regenerateApiKey: async (id: number): Promise<{ apiKey: string }> => {
		const response = await apiClient.axiosInstance.post<{ apiKey: string }>(
			`/tenants/${id}/regenerate-api-key`
		);
		return response.data;
	},
};
