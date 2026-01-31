'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { permissionsApi } from '@/lib/api/permissions';
import { rolesApi } from '@/lib/api/roles';
import { canAccess } from '@/lib/auth/canAccess';
import { useAuth } from '@/lib/auth/useAuth';
import { zodResolver } from '@hookform/resolvers/zod';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';

const createRoleSchema = z.object({
	name: z.string().min(1, 'Name is required').max(100, 'Name must be less than 100 characters'),
	description: z.string().optional(),
	permissionIds: z.array(z.number()).default([]),
});

type CreateRoleFormData = z.infer<typeof createRoleSchema>;

export default function NewRolePage() {
	const router = useRouter();
	const { user: currentUser } = useAuth();
	const queryClient = useQueryClient();
	const [error, setError] = useState<string | null>(null);

	const { data: permissions } = useQuery({
		queryKey: ['permissions'],
		queryFn: permissionsApi.getAll,
	});

	const {
		register,
		handleSubmit,
		formState: { errors },
		watch,
		setValue,
	} = useForm<CreateRoleFormData>({
		resolver: zodResolver(createRoleSchema),
		defaultValues: {
			permissionIds: [],
		},
	});

	const selectedPermissionIds = watch('permissionIds') || [];

	const createRoleMutation = useMutation({
		mutationFn: rolesApi.create,
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ['roles'] });
			router.push('/roles');
		},
		onError: (err: unknown) => {
			const error = err as { response?: { data?: { message?: string } } };
			setError(error.response?.data?.message || 'Failed to create role');
		},
	});

	if (!currentUser || !canAccess(currentUser, 'roles.manage')) {
		return (
			<div className="py-12 text-center">
				<h1 className="text-2xl font-bold text-destructive">Access Denied</h1>
				<p className="mt-4 text-muted-foreground">
					You don&apos;t have permission to create roles.
				</p>
			</div>
		);
	}

	const handleCreateRoleSubmit = (data: CreateRoleFormData) => {
		setError(null);
		createRoleMutation.mutate(data);
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
					<CardTitle>Create Role</CardTitle>
					<CardDescription>Define a new role with specific permissions</CardDescription>
				</CardHeader>
				<CardContent>
					{error && (
						<div className="mb-4 rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">
							{error}
						</div>
					)}

					<form onSubmit={handleSubmit(handleCreateRoleSubmit)} className="space-y-6">
						<div className="space-y-2">
							<Label htmlFor="name">Name *</Label>
							<Input
								{...register('name')}
								id="name"
								type="text"
								placeholder="e.g., Administrator"
							/>
							{errors.name && <p className="text-sm text-destructive">{errors.name.message}</p>}
						</div>

						<div className="space-y-2">
							<Label htmlFor="description">Description</Label>
							<textarea
								{...register('description')}
								id="description"
								rows={3}
								className="flex w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background placeholder:text-muted-foreground focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2 disabled:cursor-not-allowed disabled:opacity-50"
								placeholder="Describe the purpose of this role"
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
								{permissions?.length === 0 && (
									<p className="text-sm text-muted-foreground">No permissions available</p>
								)}
							</div>
						</div>

						<div className="flex gap-4">
							<Button type="submit" disabled={createRoleMutation.isPending}>
								{createRoleMutation.isPending ? 'Creating...' : 'Create Role'}
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
