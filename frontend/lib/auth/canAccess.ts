import { MeResponse } from '@/types/api';

export function canAccess(user: MeResponse | null, permission: string): boolean {
  if (!user) return false;
  return user.permissions.includes(permission);
}
