import { AxiosInstance } from 'axios';
import { User } from './types';

export class UsersService {
  constructor(private axios: AxiosInstance) {}

  async getAll(): Promise<User[]> {
    const response = await this.axios.get<User[]>('/users');
    return response.data;
  }

  async getById(id: number): Promise<User> {
    const response = await this.axios.get<User>(`/users/${id}`);
    return response.data;
  }

  async create(user: { email: string; password: string; isActive?: boolean; roleIds?: number[] }): Promise<User> {
    const response = await this.axios.post<User>('/users', user);
    return response.data;
  }

  async update(id: number, user: { email?: string; password?: string; isActive?: boolean; roleIds?: number[] }): Promise<User> {
    const response = await this.axios.put<User>(`/users/${id}`, user);
    return response.data;
  }

  async delete(id: number): Promise<void> {
    await this.axios.delete(`/users/${id}`);
  }
}
