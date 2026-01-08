'use client';

import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { usersApi } from '@/lib/api/users';
import { rolesApi } from '@/lib/api/roles';
import { useAuth } from '@/lib/auth/useAuth';
import { canAccess } from '@/lib/auth/canAccess';
import { useState } from 'react';
import Link from 'next/link';

export default function UsersPage() {
  const { user: currentUser } = useAuth();
  const queryClient = useQueryClient();
  const [editingUser, setEditingUser] = useState<number | null>(null);

  const { data: users, isLoading } = useQuery({
    queryKey: ['users'],
    queryFn: usersApi.getAll,
    enabled: canAccess(currentUser, 'users.view'),
  });

  const { data: roles } = useQuery({
    queryKey: ['roles'],
    queryFn: rolesApi.getAll,
  });

  const deleteMutation = useMutation({
    mutationFn: usersApi.delete,
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
    },
  });

  if (!currentUser || !canAccess(currentUser, 'users.view')) {
    return (
      <div className="text-center py-12">
        <h1 className="text-2xl font-bold text-red-600">Access Denied</h1>
        <p className="mt-4 text-gray-600">You don't have permission to view users.</p>
      </div>
    );
  }

  if (isLoading) {
    return <div className="text-center py-12">Loading users...</div>;
  }

  return (
    <div>
      <div className="flex justify-between items-center mb-6">
        <h1 className="text-3xl font-bold text-gray-900">Users</h1>
        {canAccess(currentUser, 'users.create') && (
          <Link
            href="/users/new"
            className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700"
          >
            Create User
          </Link>
        )}
      </div>

      <div className="bg-white shadow rounded-lg overflow-hidden">
        <table className="min-w-full divide-y divide-gray-200">
          <thead className="bg-gray-50">
            <tr>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Email
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Status
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Roles
              </th>
              <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                Actions
              </th>
            </tr>
          </thead>
          <tbody className="bg-white divide-y divide-gray-200">
            {users?.map((user) => (
              <tr key={user.id}>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium text-gray-900">
                  {user.email}
                </td>
                <td className="px-6 py-4 whitespace-nowrap">
                  <span
                    className={`px-2 inline-flex text-xs leading-5 font-semibold rounded-full ${
                      user.isActive
                        ? 'bg-green-100 text-green-800'
                        : 'bg-red-100 text-red-800'
                    }`}
                  >
                    {user.isActive ? 'Active' : 'Inactive'}
                  </span>
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                  {user.roles.join(', ') || 'No roles'}
                </td>
                <td className="px-6 py-4 whitespace-nowrap text-sm font-medium space-x-2">
                  {canAccess(currentUser, 'users.edit') && (
                    <Link
                      href={`/users/${user.id}/edit`}
                      className="text-blue-600 hover:text-blue-900"
                    >
                      Edit
                    </Link>
                  )}
                  {canAccess(currentUser, 'users.delete') && (
                    <button
                      onClick={() => {
                        if (confirm('Are you sure you want to delete this user?')) {
                          deleteMutation.mutate(user.id);
                        }
                      }}
                      className="text-red-600 hover:text-red-900"
                    >
                      Delete
                    </button>
                  )}
                </td>
              </tr>
            ))}
          </tbody>
        </table>
      </div>
    </div>
  );
}
