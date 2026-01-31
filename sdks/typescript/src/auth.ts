import { AxiosInstance } from 'axios';
import { LoginRequest, LoginResponse, RefreshTokenRequest, RefreshTokenResponse, User } from './types';

export class AuthService {
  constructor(private axios: AxiosInstance) {}

  async login(request: LoginRequest): Promise<LoginResponse> {
    const response = await this.axios.post<LoginResponse>('/auth/login', request);
    return response.data;
  }

  async refreshToken(request: RefreshTokenRequest): Promise<RefreshTokenResponse> {
    const response = await this.axios.post<RefreshTokenResponse>('/auth/refresh', request);
    return response.data;
  }

  async revokeToken(request: RefreshTokenRequest): Promise<void> {
    await this.axios.post('/auth/revoke', request);
  }

  async getMe(): Promise<User> {
    const response = await this.axios.get<User>('/auth/me');
    return response.data;
  }
}
