'use client';

import { useState, useEffect } from 'react';
import { useRouter, useParams } from 'next/navigation';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { rolesApi } from '@/lib/api/roles';
import { permissionsApi } from '@/lib/api/permissions';
import { useAuth } from '@/lib/auth/useAuth';
import { canAccess } from '@/lib/auth/canAccess';
import { useForm } from 'react-hook-form';
import { zodResolver } from '@hookform/resolvers/zod';
import { z } from 'zod';

const updateRoleSchema = z.object({
  name: z.string().min(1, 'Name is required').max(100, 'Name must be less than 100 characters').optional(),
  description: z.string().optional(),
  permissionIds: z.array(z.number()).optional(),
});

type UpdateRoleFormData = z.infer<typeof updateRoleSchema>;

export default function EditRolePage() {
  const router = useRouter();
  const params = useParams();
  const roleId = parseInt(params.id as string);
  const { user: currentUser } = useAuth();
  const queryClient = useQueryClient();
  const [error, setError] = useState<string | null>(null);

  const { data: role, isLoading } = useQuery({
    queryKey: ['roles', roleId],
    queryFn: () => rolesApi.getById(roleId),
  });

  const { data: permissions } = useQuery({
    queryKey: ['permissions'],
    queryFn: permissionsApi.getAll,
  });

  const {
    register,
    handleSubmit,
    formState: { errors },
    reset,
    watch,
    setValue,
  } = useForm<UpdateRoleFormData>({
    resolver: zodResolver(updateRoleSchema),
  });

  const selectedPermissionIds = watch('permissionIds') || [];

  useEffect(() => {
    if (role && permissions) {
      const rolePermissionIds = permissions
        .filter((p) => role.permissions.includes(p.key))
        .map((p) => p.id);
      reset({
        name: role.name,
        description: role.description || '',
        permissionIds: rolePermissionIds,
      });
    }
  }, [role, permissions, reset]);

  const updateMutation = useMutation({
    mutationFn: (data: UpdateRoleFormData) => rolesApi.update(roleId, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['roles'] });
      router.push('/roles');
    },
    onError: (err: any) => {
      setError(err.response?.data?.message || 'Failed to update role');
    },
  });

  if (!currentUser || !canAccess(currentUser, 'roles.manage')) {
    return (
      <div className="text-center py-12">
        <h1 className="text-2xl font-bold text-red-600">Access Denied</h1>
        <p className="mt-4 text-gray-600">You don't have permission to edit roles.</p>
      </div>
    );
  }

  if (isLoading) {
    return <div className="text-center py-12">Loading role...</div>;
  }

  if (!role) {
    return <div className="text-center py-12">Role not found</div>;
  }

  const onSubmit = (data: UpdateRoleFormData) => {
    setError(null);
    updateMutation.mutate(data);
  };

  const togglePermission = (permissionId: number) => {
    const current = selectedPermissionIds;
    const newPermissionIds = current.includes(permissionId)
      ? current.filter((id) => id !== permissionId)
      : [...current, permissionId];
    setValue('permissionIds', newPermissionIds);
  };

  return (
    <div>
      <h1 className="text-3xl font-bold text-gray-900 mb-6">Edit Role</h1>

      {error && (
        <div className="mb-4 p-3 bg-red-100 border border-red-400 text-red-700 rounded">
          {error}
        </div>
      )}

      <form onSubmit={handleSubmit(onSubmit)} className="bg-white shadow rounded-lg p-6 space-y-4">
        <div>
          <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-1">
            Name
          </label>
          <input
            {...register('name')}
            type="text"
            id="name"
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
          {errors.name && (
            <p className="mt-1 text-sm text-red-600">{errors.name.message}</p>
          )}
        </div>

        <div>
          <label htmlFor="description" className="block text-sm font-medium text-gray-700 mb-1">
            Description
          </label>
          <textarea
            {...register('description')}
            id="description"
            rows={3}
            className="w-full px-3 py-2 border border-gray-300 rounded-md focus:outline-none focus:ring-2 focus:ring-blue-500"
          />
        </div>

        <div>
          <label className="block text-sm font-medium text-gray-700 mb-2">Permissions</label>
          <div className="max-h-64 overflow-y-auto border border-gray-200 rounded-md p-4 space-y-2">
            {permissions?.map((permission) => (
              <label key={permission.id} className="flex items-start">
                <input
                  type="checkbox"
                  checked={selectedPermissionIds.includes(permission.id)}
                  onChange={() => togglePermission(permission.id)}
                  className="mt-1 rounded border-gray-300 text-blue-600 focus:ring-blue-500"
                />
                <div className="ml-2">
                  <span className="text-sm font-medium text-gray-700">{permission.key}</span>
                  {permission.description && (
                    <p className="text-xs text-gray-500">{permission.description}</p>
                  )}
                </div>
              </label>
            ))}
          </div>
          <input
            type="hidden"
            {...register('permissionIds')}
          />
        </div>

        <div className="flex space-x-4">
          <button
            type="submit"
            disabled={updateMutation.isPending}
            className="px-4 py-2 bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50"
          >
            {updateMutation.isPending ? 'Updating...' : 'Update Role'}
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
