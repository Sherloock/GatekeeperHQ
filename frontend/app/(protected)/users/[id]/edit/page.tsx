'use client';

import { useState, useEffect } from 'react';
import { useRouter, useParams } from 'next/navigation';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { usersApi } from '@/lib/api/users';
import { rolesApi } from '@/lib/api/roles';
import { useAuth } from '@/lib/auth/useAuth';
import { canAccess } from '@/lib/auth/canAccess';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

const updateUserSchema = z.object({
  email: z.string().email('Invalid email address').optional(),
  password: z.string().min(6, 'Password must be at least 6 characters').optional().or(z.literal('')),
  isActive: z.boolean().optional(),
  roleIds: z.array(z.number()).optional(),
});

type UpdateUserFormData = z.infer<typeof updateUserSchema>;

export default function EditUserPage() {
  const router = useRouter();
  const params = useParams();
  const userId = parseInt(params.id as string);
  const { user: currentUser } = useAuth();
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);

  const { data: user, isLoading } = useQuery({
    queryKey: ['users', userId],
    queryFn: () => usersApi.getById(userId),
  });

  const { data: roles } = useQuery({
    queryKey: ['roles'],
    queryFn: rolesApi.getAll,
  });

  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
    watch,
    setValue,
  } = useForm<UpdateUserFormData>({
    resolver: zodResolver(updateUserSchema),
  });

  const selectedRoleIds = watch('roleIds') || [];

  useEffect(() => {
    if (user && roles) {
      const userRoleIds = roles
        .filter((r) => user.roles.includes(r.name))
        .map((r) => r.id);
      reset({
        email: user.email,
        isActive: user.isActive,
        roleIds: userRoleIds,
      });
    }
  }, [user, roles, reset]);

  const updateMutation = useMutation({
    mutationFn: (data: UpdateUserFormData) => usersApi.update(userId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['users'] });
      router.push('/users');
    },
    onError: (err: any) => {
      setError(err.response?.data?.message || 'Failed to update user');
    },
  });

  if (!currentUser || !canAccess(currentUser, 'users.edit')) {
    return (
      <div className="text-center py-12">
        <h1 className="text-2xl font-bold text-red-600">Access Denied</h1>
        <p className="mt-4 text-gray-600">You don't have permission to edit users.</p>
      </div>
    );
  }

  if (isLoading) {
    return <div className="text-center py-12">Loading user...</div>;
  }

  if (!user) {
    return <div className="text-center py-12">User not found</div>;
  }

  const onSubmit = (data: UpdateUserFormData) => {
    setError(null);
    // Remove empty password
    if (data.password === '') {
      delete data.password;
    }
    updateMutation.mutate(data);
  };

  const toggleRole = (roleId: number) => {
    const current = selectedRoleIds;
    const newRoleIds = current.includes(roleId)
      ? current.filter((id) => id !== roleId)
      : [...current, roleId];
    setValue('roleIds', newRoleIds);
  };

  return (
    <div>
      <h1 className="text-3xl font-bold text-gray-900 mb-6">Edit User</h1>

      {error && (
        <div className="mb-4 p-3 bg-red-100 border border-red-400 text-red-700 rounded">
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)} className="bg-white shadow rounded-lg p-6 space-y-4">
        <div>
          <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
            Email
          </label>
          <input
            {...register('email')}
            type="email"
            id="email"
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          {errors.email && (
            <p className="mt-1 text-sm text-red-600">{errors.email.message}</p>
          )}
        </div>

        <div>
          <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1">
            Password (leave empty to keep current)
          </label>
          <input
            {...register('password')}
            type="password"
            id="password"
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          {errors.password && (
            <p className="mt-1 text-sm text-red-600">{errors.password.message}</p>
          )}
        </div>

        <div>
          <label className="flex items-center">
            <input
              {...register('isActive')}
              type="checkbox"
              className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
            />
            <span className="ml-2 text-sm text-gray-700">Active</span>
          </label>
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Roles</label>
          <div className="space-y-2">
            {roles?.map((role) => (
              <label key={role.id} className="flex items-center">
                <input
                  type="checkbox"
                  checked={selectedRoleIds.includes(role.id)}
                  onChange={() => toggleRole(role.id)}
                  className="rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <span className="ml-2 text-sm text-gray-700">{role.name}</span>
              </label>
            ))}
          </div>
          <input type="hidden" {...register('roleIds')} />
        </div>

        <div className="flex space-x-4">
          <button
            type="submit"
            disabled={updateMutation.isPending}
            className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50"
          >
            {updateMutation.isPending ? 'Updating...' : 'Update User'}
          </button>
          <button
            type="button"
            onClick={() => router.back()}
            className="px-4 py-2 bg-gray-200 text-gray-700 rounded-md hover:bg-gray-300"
          >
            Cancel
          </button>
        </div>
      </form>
    </div>
  );
}
