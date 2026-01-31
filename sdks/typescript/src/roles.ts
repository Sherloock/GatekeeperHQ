import { AxiosInstance } from 'axios';
import { Role, Permission } from './types';

export class RolesService {
  constructor(private axios: AxiosInstance) {}

  async getAll(): Promise<Role[]> {
    const response = await this.axios.get<Role[]>('/roles');
    return response.data;
  }

  async getById(id: number): Promise<Role> {
    const response = await this.axios.get<Role>(`/roles/${id}`);
    return response.data;
  }

  async create(role: { name: string; description?: string; permissionIds?: number[] }): Promise<Role> {
    const response = await this.axios.post<Role>('/roles', role);
    return response.data;
  }

  async update(id: number, role: { name?: string; description?: string; permissionIds?: number[] }): Promise<Role> {
    const response = await this.axios.put<Role>(`/roles/${id}`, role);
    return response.data;
  }

  async delete(id: number): Promise<void> {
    await this.axios.delete(`/roles/${id}`);
  }

  async getPermissions(roleId: number): Promise<Permission[]> {
    const response = await this.axios.get<Permission[]>(`/roles/${roleId}/permissions`);
    return response.data;
  }

  async addPermission(roleId: number, permissionId: number): Promise<void> {
    await this.axios.post(`/roles/${roleId}/permissions`, { permissionId });
  }

  async removePermission(roleId: number, permissionId: number): Promise<void> {
    await this.axios.delete(`/roles/${roleId}/permissions/${permissionId}`);
  }
}
