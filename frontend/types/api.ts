export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  token: string;
  userId: number;
  email: string;
  permissions: string[];
}

export interface MeResponse {
  id: number;
  email: string;
  isActive: boolean;
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
