import { AxiosInstance } from "axios";
import {
	AcceptInvitationRequest,
	AcceptInvitationResponse,
	CreateInvitationRequest,
	Invitation,
	ValidateInvitationResponse,
} from "./types";

export class InvitationsService {
	constructor(private axios: AxiosInstance) {}

	async getByTenant(tenantId: number): Promise<Invitation[]> {
		const response = await this.axios.get<Invitation[]>(
			`/invitations/tenants/${tenantId}`,
		);
		return response.data;
	}

	async create(
		tenantId: number,
		data: CreateInvitationRequest,
	): Promise<Invitation> {
		const response = await this.axios.post<Invitation>(
			`/invitations/tenants/${tenantId}`,
			data,
		);
		return response.data;
	}

	async revoke(id: number): Promise<void> {
		await this.axios.delete(`/invitations/${id}`);
	}

	async validate(token: string): Promise<ValidateInvitationResponse> {
		const response = await this.axios.get<ValidateInvitationResponse>(
			`/invitations/${token}/validate`,
		);
		return response.data;
	}

	async accept(
		token: string,
		data: AcceptInvitationRequest,
	): Promise<AcceptInvitationResponse> {
		const response = await this.axios.post<AcceptInvitationResponse>(
			`/invitations/${token}/accept`,
			data,
		);
		return response.data;
	}
}
