import { MeResponse } from '@/types/api';

export function canAccess(user: MeResponse | null, permission: string): boolean {
	if (!user) return false;
	// Super Admin has access to everything
	if (user.isSuperAdmin) return true;
	return user.permissions.includes(permission);
}
