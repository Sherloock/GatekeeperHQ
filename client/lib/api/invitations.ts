import type {
	AcceptInvitationRequest,
	AcceptInvitationResponse,
	CreateInvitationRequest,
	InvitationDto,
	ValidateInvitationResponse,
} from '@/types/api';
import { apiClient } from './client';

export const invitationsApi = {
	getByTenant: async (tenantId: number): Promise<InvitationDto[]> => {
		const response = await apiClient.axiosInstance.get<InvitationDto[]>(
			`/invitations/tenants/${tenantId}`
		);
		return response.data;
	},

	create: async (tenantId: number, data: CreateInvitationRequest): Promise<InvitationDto> => {
		const response = await apiClient.axiosInstance.post<InvitationDto>(
			`/invitations/tenants/${tenantId}`,
			data
		);
		return response.data;
	},

	revoke: async (id: number): Promise<void> => {
		await apiClient.axiosInstance.delete(`/invitations/${id}`);
	},

	// Public endpoints (no auth required)
	validate: async (token: string): Promise<ValidateInvitationResponse> => {
		const response = await apiClient.axiosInstance.get<ValidateInvitationResponse>(
			`/invitations/${token}/validate`
		);
		return response.data;
	},

	accept: async (
		token: string,
		data: AcceptInvitationRequest
	): Promise<AcceptInvitationResponse> => {
		const response = await apiClient.axiosInstance.post<AcceptInvitationResponse>(
			`/invitations/${token}/accept`,
			data
		);
		return response.data;
	},
};
