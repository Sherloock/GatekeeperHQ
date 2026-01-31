export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  refreshToken: string;
  userId: number;
  email: string;
  permissions: string[];
}

export interface RefreshTokenRequest {
  refreshToken: string;
}

export interface RefreshTokenResponse {
  token: string;
  refreshToken: string;
}

export interface User {
  id: number;
  email: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  roles: string[];
}

export interface Role {
  id: number;
  name: string;
  description?: string;
  createdAt: string;
  permissions: string[];
}

export interface Permission {
  id: number;
  key: string;
  description?: string;
}

export interface Webhook {
  id: number;
  url: string;
  events: string[];
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface CreateWebhookRequest {
  url: string;
  secret?: string;
  events: string[];
  isActive?: boolean;
}

export interface ApiKey {
  id: number;
  name: string;
  permissions: string[];
  isActive: boolean;
  expiresAt?: string;
  createdAt: string;
  updatedAt: string;
  lastUsedAt?: string;
}

export interface CreateApiKeyRequest {
  name: string;
  permissions?: string[];
  isActive?: boolean;
  expiresAt?: string;
}

export interface CreateApiKeyResponse extends ApiKey {
  key: string; // Only returned on creation
}

export interface ClientOptions {
  baseUrl: string;
  apiKey?: string;
  token?: string;
  version?: string;
  onTokenExpired?: () => void;
}
