'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { usersApi } from '@/lib/api/users';
import { canAccess } from '@/lib/auth/canAccess';
import { useAuth } from '@/lib/auth/useAuth';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Building2, Pencil, Plus, Trash2 } from 'lucide-react';
import Link from 'next/link';

export default function UsersPage() {
	const { user: currentUser, selectedTenantId } = useAuth();
	const queryClient = useQueryClient();

	// Super admins need to select a tenant to view tenant-scoped resources
	const needsTenantSelection = currentUser?.isSuperAdmin && !selectedTenantId;

	const { data: users, isLoading } = useQuery({
		queryKey: ['users', selectedTenantId],
		queryFn: usersApi.getAll,
		enabled: canAccess(currentUser, 'users.view') && !needsTenantSelection,
	});

	const deleteUserMutation = useMutation({
		mutationFn: usersApi.delete,
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ['users'] });
		},
	});

	const handleDeleteUser = (userId: number, userEmail: string) => {
		if (confirm(`Are you sure you want to delete "${userEmail}"?`)) {
			deleteUserMutation.mutate(userId);
		}
	};

	if (!currentUser || !canAccess(currentUser, 'users.view')) {
		return (
			<div className="py-12 text-center">
				<h1 className="text-2xl font-bold text-destructive">Access Denied</h1>
				<p className="mt-4 text-muted-foreground">You don&apos;t have permission to view users.</p>
			</div>
		);
	}

	if (needsTenantSelection) {
		return (
			<div className="py-12 text-center">
				<Building2 className="mx-auto h-12 w-12 text-muted-foreground" />
				<h1 className="mt-4 text-2xl font-bold">Select a Tenant</h1>
				<p className="mt-2 text-muted-foreground">
					Please select a tenant from the header to view and manage users.
				</p>
			</div>
		);
	}

	return (
		<div className="space-y-6">
			<div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
				<div>
					<h1 className="text-2xl font-bold tracking-tight">Users</h1>
					<p className="text-muted-foreground">Manage user accounts and their roles</p>
				</div>
				{canAccess(currentUser, 'users.create') && (
					<Button asChild>
						<Link href="/users/new">
							<Plus className="mr-2 h-4 w-4" />
							Create User
						</Link>
					</Button>
				)}
			</div>

			<Card>
				<CardHeader>
					<CardTitle className="text-lg">All Users</CardTitle>
					<CardDescription>
						{isLoading ? 'Loading...' : `${users?.length || 0} users total`}
					</CardDescription>
				</CardHeader>
				<CardContent>
					{isLoading ? (
						<div className="space-y-4">
							{[...Array(3)].map((_, i) => (
								<div key={i} className="flex items-center justify-between">
									<div className="space-y-2">
										<Skeleton className="h-4 w-48" />
										<Skeleton className="h-3 w-24" />
									</div>
									<Skeleton className="h-8 w-20" />
								</div>
							))}
						</div>
					) : (
						<div className="overflow-x-auto">
							<table className="w-full">
								<thead>
									<tr className="border-b text-left text-sm font-medium text-muted-foreground">
										<th className="pb-3 pr-4">Email</th>
										<th className="pb-3 pr-4">Status</th>
										<th className="pb-3 pr-4">Roles</th>
										<th className="pb-3 text-right">Actions</th>
									</tr>
								</thead>
								<tbody className="divide-y">
									{users?.map((user) => (
										<tr key={user.id} className="text-sm">
											<td className="py-3 pr-4 font-medium">{user.email}</td>
											<td className="py-3 pr-4">
												<Badge variant={user.isActive ? 'success' : 'destructive'}>
													{user.isActive ? 'Active' : 'Inactive'}
												</Badge>
											</td>
											<td className="py-3 pr-4">
												<div className="flex flex-wrap gap-1">
													{user.roles.length > 0 ? (
														user.roles.map((role) => (
															<Badge key={role} variant="secondary">
																{role}
															</Badge>
														))
													) : (
														<span className="text-muted-foreground">No roles</span>
													)}
												</div>
											</td>
											<td className="py-3 text-right">
												<div className="flex items-center justify-end gap-2">
													{canAccess(currentUser, 'users.edit') && (
														<Button variant="ghost" size="sm" asChild>
															<Link href={`/users/${user.id}/edit`}>
																<Pencil className="h-4 w-4" />
															</Link>
														</Button>
													)}
													{canAccess(currentUser, 'users.delete') && (
														<Button
															variant="ghost"
															size="sm"
															onClick={() => handleDeleteUser(user.id, user.email)}
															disabled={deleteUserMutation.isPending}
														>
															<Trash2 className="h-4 w-4 text-destructive" />
														</Button>
													)}
												</div>
											</td>
										</tr>
									))}
								</tbody>
							</table>
						</div>
					)}
				</CardContent>
			</Card>
		</div>
	);
}
