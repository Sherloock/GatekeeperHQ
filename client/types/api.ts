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

export interface UserDto {
	id: number;
	email: string;
	isActive: boolean;
	createdAt: string;
	updatedAt: string;
	roles: string[];
}

export interface CreateUserRequest {
	email: string;
	password: string;
	isActive: boolean;
	roleIds: number[];
}

export interface UpdateUserRequest {
	email?: string;
	password?: string;
	isActive?: boolean;
	roleIds?: number[];
}

export interface RoleDto {
	id: number;
	name: string;
	description?: string;
	createdAt: string;
	permissions: string[];
}

export interface CreateRoleRequest {
	name: string;
	description?: string;
	permissionIds: number[];
}

export interface UpdateRoleRequest {
	name?: string;
	description?: string;
	permissionIds?: number[];
}

export interface PermissionDto {
	id: number;
	key: string;
	description?: string;
}

export interface AddPermissionRequest {
	permissionId: number;
}

export interface CreatePermissionRequest {
	key: string;
	description?: string;
}

export interface UpdatePermissionRequest {
	key?: string;
	description?: string;
}

// Tenant types
export interface TenantDto {
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
export interface InvitationDto {
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
	status: 'pending' | 'accepted' | 'expired';
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
