import { AxiosInstance } from "axios";
import { CreateTenantRequest, Tenant, UpdateTenantRequest } from "./types";

export class TenantsService {
	constructor(private axios: AxiosInstance) {}

	async getAll(): Promise<Tenant[]> {
		const response = await this.axios.get<Tenant[]>("/tenants");
		return response.data;
	}

	async getById(id: number): Promise<Tenant> {
		const response = await this.axios.get<Tenant>(`/tenants/${id}`);
		return response.data;
	}

	async create(data: CreateTenantRequest): Promise<Tenant> {
		const response = await this.axios.post<Tenant>("/tenants", data);
		return response.data;
	}

	async update(id: number, data: UpdateTenantRequest): Promise<Tenant> {
		const response = await this.axios.put<Tenant>(`/tenants/${id}`, data);
		return response.data;
	}

	async delete(id: number): Promise<void> {
		await this.axios.delete(`/tenants/${id}`);
	}

	async regenerateApiKey(id: number): Promise<{ apiKey: string }> {
		const response = await this.axios.post<{ apiKey: string }>(
			`/tenants/${id}/regenerate-api-key`,
		);
		return response.data;
	}
}
