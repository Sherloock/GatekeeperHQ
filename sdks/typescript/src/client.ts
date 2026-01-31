import axios, { AxiosError, AxiosInstance, AxiosResponse, InternalAxiosRequestConfig } from 'axios';
import { AuthService } from './auth';
import { RolesService } from './roles';
import { ClientOptions } from './types';
import { UsersService } from './users';
import { WebhooksService } from './webhooks';

export class GatekeeperHQClient {
  private axiosInstance: AxiosInstance;
  public auth: AuthService;
  public users: UsersService;
  public roles: RolesService;
  public webhooks: WebhooksService;
  private onTokenExpired?: () => void;

  constructor(options: ClientOptions) {
    const baseURL = `${options.baseUrl.replace(/\/$/, '')}/api/v${options.version || '1'}`;
    this.onTokenExpired = options.onTokenExpired;

    this.axiosInstance = axios.create({
      baseURL,
      headers: {
        'Content-Type': 'application/json',
      },
    });

    // Add API key or token to requests
    this.axiosInstance.interceptors.request.use((config: InternalAxiosRequestConfig) => {
      if (options.apiKey) {
        config.headers['X-API-Key'] = options.apiKey;
      } else if (options.token) {
        config.headers['Authorization'] = `Bearer ${options.token}`;
      }
      return config;
    });

    // Handle 401 errors
    this.axiosInstance.interceptors.response.use(
      (response: AxiosResponse) => response,
      (error: AxiosError) => {
        if (error.response?.status === 401) {
          // Token expired or invalid
          if (this.onTokenExpired) {
            this.onTokenExpired();
          }
        }
        return Promise.reject(error);
      }
    );

    // Initialize services
    this.auth = new AuthService(this.axiosInstance);
    this.users = new UsersService(this.axiosInstance);
    this.roles = new RolesService(this.axiosInstance);
    this.webhooks = new WebhooksService(this.axiosInstance);
  }

  setToken(token: string): void {
    this.axiosInstance.defaults.headers.common['Authorization'] = `Bearer ${token}`;
  }

  removeToken(): void {
    delete this.axiosInstance.defaults.headers.common['Authorization'];
  }
}
