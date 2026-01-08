'use client';

import { useAuth } from '@/lib/auth/useAuth';
import { canAccess } from '@/lib/auth/canAccess';
import { useRouter } from 'next/navigation';

export default function DashboardPage() {
  const { user } = useAuth();
  const router = useRouter();

  if (!user || !canAccess(user, 'dashboard.access')) {
    return (
      <div className="text-center py-12">
        <h1 className="text-2xl font-bold text-red-600">Access Denied</h1>
        <p className="mt-4 text-gray-600">You don't have permission to access the dashboard.</p>
      </div>
    );
  }

  return (
    <div>
      <h1 className="text-3xl font-bold text-gray-900 mb-6">Dashboard</h1>
      <div className="bg-white shadow rounded-lg p-6">
        <h2 className="text-xl font-semibold mb-4">Welcome, {user.email}!</h2>
        <div className="space-y-4">
          <div>
            <h3 className="font-medium text-gray-700">Your Roles:</h3>
            <ul className="list-disc list-inside mt-2">
              {user.roles.map((role) => (
                <li key={role} className="text-gray-600">{role}</li>
              ))}
            </ul>
          </div>
          <div>
            <h3 className="font-medium text-gray-700">Your Permissions:</h3>
            <ul className="list-disc list-inside mt-2">
              {user.permissions.map((permission) => (
                <li key={permission} className="text-gray-600">{permission}</li>
              ))}
            </ul>
          </div>
        </div>
      </div>
    </div>
  );
}
