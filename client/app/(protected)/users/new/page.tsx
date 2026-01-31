'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { rolesApi } from '@/lib/api/roles';
import { usersApi } from '@/lib/api/users';
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

const createUserSchema = z.object({
	email: z.string().email('Invalid email address'),
	password: z
		.string()
		.min(8, 'Password must be at least 8 characters long')
		.max(128, 'Password must be less than 128 characters')
		.regex(
			/^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,}$/,
			'Password must contain at least one uppercase letter, one lowercase letter, one number, and one special character'
		),
	isActive: z.boolean().default(true),
	roleIds: z.array(z.number()).default([]),
});

type CreateUserFormData = z.infer<typeof createUserSchema>;

export default function NewUserPage() {
	const router = useRouter();
	const { user: currentUser } = useAuth();
	const queryClient = useQueryClient();
	const [error, setError] = useState<string | null>(null);

	const { data: roles } = useQuery({
		queryKey: ['roles'],
		queryFn: rolesApi.getAll,
	});

	const {
		register,
		handleSubmit,
		formState: { errors },
		watch,
		setValue,
	} = useForm<CreateUserFormData>({
		resolver: zodResolver(createUserSchema),
		defaultValues: {
			isActive: true,
			roleIds: [],
		},
	});

	const selectedRoleIds = watch('roleIds') || [];
	const isActive = watch('isActive');

	const createUserMutation = useMutation({
		mutationFn: usersApi.create,
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ['users'] });
			router.push('/users');
		},
		onError: (err: unknown) => {
			const error = err as { response?: { data?: { message?: string } } };
			setError(error.response?.data?.message || 'Failed to create user');
		},
	});

	if (!currentUser || !canAccess(currentUser, 'users.create')) {
		return (
			<div className="py-12 text-center">
				<h1 className="text-2xl font-bold text-destructive">Access Denied</h1>
				<p className="mt-4 text-muted-foreground">
					You don&apos;t have permission to create users.
				</p>
			</div>
		);
	}

	const handleCreateUserSubmit = (data: CreateUserFormData) => {
		setError(null);
		createUserMutation.mutate(data);
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
					<CardTitle>Create User</CardTitle>
					<CardDescription>Add a new user to the system</CardDescription>
				</CardHeader>
				<CardContent>
					{error && (
						<div className="mb-4 rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">
							{error}
						</div>
					)}

					<form onSubmit={handleSubmit(handleCreateUserSubmit)} className="space-y-6">
						<div className="space-y-2">
							<Label htmlFor="email">Email *</Label>
							<Input
								{...register('email')}
								id="email"
								type="email"
								placeholder="user@example.com"
							/>
							{errors.email && <p className="text-sm text-destructive">{errors.email.message}</p>}
						</div>

						<div className="space-y-2">
							<Label htmlFor="password">Password *</Label>
							<Input
								{...register('password')}
								id="password"
								type="password"
								placeholder="Enter a strong password"
							/>
							{errors.password && (
								<p className="text-sm text-destructive">{errors.password.message}</p>
							)}
							<p className="text-xs text-muted-foreground">
								Must contain uppercase, lowercase, number, and special character
							</p>
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
								{roles?.length === 0 && (
									<p className="text-sm text-muted-foreground">No roles available</p>
								)}
							</div>
						</div>

						<div className="flex gap-4">
							<Button type="submit" disabled={createUserMutation.isPending}>
								{createUserMutation.isPending ? 'Creating...' : 'Create User'}
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
