import type { LoginRequest, LoginResponse, MeResponse } from '@/types/api';
import { apiClient } from './client';

export const authApi = {
	login: async (data: LoginRequest): Promise<LoginResponse> => {
		// Add tenantId query parameter for development (default tenant ID is 1)
		const tenantId = process.env.NEXT_PUBLIC_TENANT_ID || '1';
		const response = await apiClient.axiosInstance.post<LoginResponse>(
			`/auth/login?tenantId=${tenantId}`,
			data
		);
		if (response.data.token) {
			apiClient.setToken(response.data.token);
		}
		return response.data;
	},

	getMe: async (): Promise<MeResponse> => {
		// Add tenantId query parameter for development (default tenant ID is 1)
		const tenantId = process.env.NEXT_PUBLIC_TENANT_ID || '1';
		const response = await apiClient.axiosInstance.get<MeResponse>(`/auth/me?tenantId=${tenantId}`);
		return response.data;
	},

	logout: () => {
		apiClient.removeToken();
	},
};
