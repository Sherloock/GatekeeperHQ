'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import { permissionsApi } from '@/lib/api/permissions';
import { rolesApi } from '@/lib/api/roles';
import { canAccess } from '@/lib/auth/canAccess';
import { useAuth } from '@/lib/auth/useAuth';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft } from 'lucide-react';
import Link from 'next/link';
import { useParams, useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';

const updateRoleSchema = z.object({
	name: z
		.string()
		.min(1, 'Name is required')
		.max(100, 'Name must be less than 100 characters')
		.optional(),
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

	const updateRoleMutation = useMutation({
		mutationFn: (data: UpdateRoleFormData) => rolesApi.update(roleId, data),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ['roles'] });
			router.push('/roles');
		},
		onError: (err: unknown) => {
			const error = err as { response?: { data?: { message?: string } } };
			setError(error.response?.data?.message || 'Failed to update role');
		},
	});

	if (!currentUser || !canAccess(currentUser, 'roles.manage')) {
		return (
			<div className="py-12 text-center">
				<h1 className="text-2xl font-bold text-destructive">Access Denied</h1>
				<p className="mt-4 text-muted-foreground">You don&apos;t have permission to edit roles.</p>
			</div>
		);
	}

	if (isLoading) {
		return (
			<div className="mx-auto max-w-2xl space-y-6">
				<Skeleton className="h-10 w-24" />
				<Card>
					<CardHeader>
						<Skeleton className="h-6 w-32" />
						<Skeleton className="h-4 w-48" />
					</CardHeader>
					<CardContent className="space-y-4">
						<Skeleton className="h-10 w-full" />
						<Skeleton className="h-20 w-full" />
						<Skeleton className="h-48 w-full" />
					</CardContent>
				</Card>
			</div>
		);
	}

	if (!role) {
		return (
			<div className="py-12 text-center">
				<h1 className="text-2xl font-bold text-destructive">Role Not Found</h1>
				<Button variant="link" asChild className="mt-4">
					<Link href="/roles">Back to Roles</Link>
				</Button>
			</div>
		);
	}

	const handleUpdateRoleSubmit = (data: UpdateRoleFormData) => {
		setError(null);
		updateRoleMutation.mutate(data);
	};

	const togglePermission = (permissionId: number) => {
		const current = selectedPermissionIds;
		const newPermissionIds = current.includes(permissionId)
			? current.filter((id) => id !== permissionId)
			: [...current, permissionId];
		setValue('permissionIds', newPermissionIds);
	};

	return (
		<div className="mx-auto max-w-2xl space-y-6">
			<div className="flex items-center gap-4">
				<Button variant="ghost" size="sm" asChild>
					<Link href="/roles">
						<ArrowLeft className="mr-2 h-4 w-4" />
						Back
					</Link>
				</Button>
			</div>

			<Card>
				<CardHeader>
					<CardTitle>Edit Role</CardTitle>
					<CardDescription>Update role information and permissions</CardDescription>
				</CardHeader>
				<CardContent>
					{error && (
						<div className="mb-4 rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">
							{error}
						</div>
					)}

					<form onSubmit={handleSubmit(handleUpdateRoleSubmit)} className="space-y-6">
						<div className="space-y-2">
							<Label htmlFor="name">Name</Label>
							<Input {...register('name')} id="name" type="text" />
							{errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
						</div>

						<div className="space-y-2">
							<Label htmlFor="description">Description</Label>
							<textarea
								{...register('description')}
								id="description"
								rows={3}
								className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
							/>
						</div>

						<div className="space-y-2">
							<Label>Permissions</Label>
							<div className="max-h-64 space-y-3 overflow-y-auto rounded-md border p-4">
								{permissions?.map((permission) => (
									<div key={permission.id} className="flex items-start space-x-2">
										<Checkbox
											id={`permission-${permission.id}`}
											checked={selectedPermissionIds.includes(permission.id)}
											onCheckedChange={() => togglePermission(permission.id)}
											className="mt-0.5"
										/>
										<div className="grid gap-0.5 leading-none">
											<Label
												htmlFor={`permission-${permission.id}`}
												className="cursor-pointer font-medium"
											>
												{permission.key}
											</Label>
											{permission.description && (
												<p className="text-xs text-muted-foreground">{permission.description}</p>
											)}
										</div>
									</div>
								))}
							</div>
						</div>

						<div className="flex gap-4">
							<Button type="submit" disabled={updateRoleMutation.isPending}>
								{updateRoleMutation.isPending ? 'Updating...' : 'Update Role'}
							</Button>
							<Button type="button" variant="outline" onClick={() => router.back()}>
								Cancel
							</Button>
						</div>
					</form>
				</CardContent>
			</Card>
		</div>
	);
}
