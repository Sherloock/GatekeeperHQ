import { AxiosInstance } from "axios";
import {
	CreatePermissionRequest,
	Permission,
	UpdatePermissionRequest,
} from "./types";

export class PermissionsService {
	constructor(private axios: AxiosInstance) {}

	async getAll(): Promise<Permission[]> {
		const response = await this.axios.get<Permission[]>("/permissions");
		return response.data;
	}

	async getById(id: number): Promise<Permission> {
		const response = await this.axios.get<Permission>(`/permissions/${id}`);
		return response.data;
	}

	async create(data: CreatePermissionRequest): Promise<Permission> {
		const response = await this.axios.post<Permission>("/permissions", data);
		return response.data;
	}

	async update(id: number, data: UpdatePermissionRequest): Promise<Permission> {
		const response = await this.axios.put<Permission>(
			`/permissions/${id}`,
			data,
		);
		return response.data;
	}

	async delete(id: number): Promise<void> {
		await this.axios.delete(`/permissions/${id}`);
	}
}
