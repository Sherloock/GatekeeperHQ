'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import { rolesApi } from '@/lib/api/roles';
import { usersApi } from '@/lib/api/users';
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

const updateUserSchema = z.object({
	email: z.string().email('Invalid email address').optional(),
	password: z
		.string()
		.optional()
		.refine(
			(val) => !val || val === '' || (val.length >= 8 && val.length <= 128),
			'Password must be between 8 and 128 characters'
		)
		.refine(
			(val) =>
				!val ||
				val === '' ||
				/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/.test(val),
			'Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character'
		)
		.or(z.literal('')),
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
	const isActive = watch('isActive');

	useEffect(() => {
		if (user && roles) {
			const userRoleIds = roles.filter((r) => user.roles.includes(r.name)).map((r) => r.id);
			reset({
				email: user.email,
				isActive: user.isActive,
				roleIds: userRoleIds,
			});
		}
	}, [user, roles, reset]);

	const updateUserMutation = useMutation({
		mutationFn: (data: UpdateUserFormData) => usersApi.update(userId, data),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ['users'] });
			router.push('/users');
		},
		onError: (err: unknown) => {
			const error = err as { response?: { data?: { message?: string } } };
			setError(error.response?.data?.message || 'Failed to update user');
		},
	});

	if (!currentUser || !canAccess(currentUser, 'users.edit')) {
		return (
			<div className="py-12 text-center">
				<h1 className="text-2xl font-bold text-destructive">Access Denied</h1>
				<p className="mt-4 text-muted-foreground">You don&apos;t have permission to edit users.</p>
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
						<Skeleton className="h-10 w-full" />
						<Skeleton className="h-32 w-full" />
					</CardContent>
				</Card>
			</div>
		);
	}

	if (!user) {
		return (
			<div className="py-12 text-center">
				<h1 className="text-2xl font-bold text-destructive">User Not Found</h1>
				<Button variant="link" asChild className="mt-4">
					<Link href="/users">Back to Users</Link>
				</Button>
			</div>
		);
	}

	const handleUpdateUserSubmit = (data: UpdateUserFormData) => {
		setError(null);
		if (data.password === '') {
			delete data.password;
		}
		updateUserMutation.mutate(data);
	};

	const toggleRole = (roleId: number) => {
		const current = selectedRoleIds;
		const newRoleIds = current.includes(roleId)
			? current.filter((id) => id !== roleId)
			: [...current, roleId];
		setValue('roleIds', newRoleIds);
	};

	return (
		<div className="mx-auto max-w-2xl space-y-6">
			<div className="flex items-center gap-4">
				<Button variant="ghost" size="sm" asChild>
					<Link href="/users">
						<ArrowLeft className="mr-2 h-4 w-4" />
						Back
					</Link>
				</Button>
			</div>

			<Card>
				<CardHeader>
					<CardTitle>Edit User</CardTitle>
					<CardDescription>Update user information and roles</CardDescription>
				</CardHeader>
				<CardContent>
					{error && (
						<div className="mb-4 rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">
							{error}
						</div>
					)}

					<form onSubmit={handleSubmit(handleUpdateUserSubmit)} className="space-y-6">
						<div className="space-y-2">
							<Label htmlFor="email">Email</Label>
							<Input {...register('email')} id="email" type="email" />
							{errors.email && <p className="text-sm text-destructive">{errors.email.message}</p>}
						</div>

						<div className="space-y-2">
							<Label htmlFor="password">Password</Label>
							<Input
								{...register('password')}
								id="password"
								type="password"
								placeholder="Leave empty to keep current"
							/>
							{errors.password && (
								<p className="text-sm text-destructive">{errors.password.message}</p>
							)}
						</div>

						<div className="flex items-center space-x-2">
							<Checkbox
								id="isActive"
								checked={isActive}
								onCheckedChange={(checked) => setValue('isActive', checked === true)}
							/>
							<Label htmlFor="isActive" className="cursor-pointer">
								Active
							</Label>
						</div>

						<div className="space-y-2">
							<Label>Roles</Label>
							<div className="max-h-48 space-y-2 overflow-y-auto rounded-md border p-4">
								{roles?.map((role) => (
									<div key={role.id} className="flex items-center space-x-2">
										<Checkbox
											id={`role-${role.id}`}
											checked={selectedRoleIds.includes(role.id)}
											onCheckedChange={() => toggleRole(role.id)}
										/>
										<Label htmlFor={`role-${role.id}`} className="cursor-pointer">
											{role.name}
										</Label>
									</div>
								))}
							</div>
						</div>

						<div className="flex gap-4">
							<Button type="submit" disabled={updateUserMutation.isPending}>
								{updateUserMutation.isPending ? 'Updating...' : 'Update User'}
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
