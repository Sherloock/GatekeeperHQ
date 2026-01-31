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
	isSuperAdmin: boolean;
}

export interface MeResponse {
	id: number;
	email: string;
	isActive: boolean;
	isSuperAdmin: boolean;
	roles: string[];
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

// Tenant types
export interface Tenant {
	id: number;
	name: string;
	apiKey?: string;
	isActive: boolean;
	createdAt: string;
	updatedAt: string;
}

export interface CreateTenantRequest {
	name: string;
	isActive?: boolean;
}

export interface UpdateTenantRequest {
	name?: string;
	isActive?: boolean;
}

// Invitation types
export interface Invitation {
	id: number;
	tenantId: number;
	tenantName: string;
	email: string;
	token: string;
	roleId?: number;
	roleName?: string;
	expiresAt: string;
	acceptedAt?: string;
	createdByUserId: number;
	createdByEmail: string;
	createdAt: string;
	status: "pending" | "accepted" | "expired";
}

export interface CreateInvitationRequest {
	email: string;
	roleId?: number;
}

export interface ValidateInvitationResponse {
	valid: boolean;
	email?: string;
	tenantName?: string;
	roleName?: string;
	expiresAt?: string;
	error?: string;
}

export interface AcceptInvitationRequest {
	password: string;
}

export interface AcceptInvitationResponse {
	success: boolean;
	error?: string;
	userId?: number;
	email?: string;
	tenantId?: number;
	tenantName?: string;
}

// Permission CRUD types
export interface CreatePermissionRequest {
	key: string;
	description?: string;
}

export interface UpdatePermissionRequest {
	key?: string;
	description?: string;
}
