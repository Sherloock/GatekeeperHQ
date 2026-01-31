'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { rolesApi } from '@/lib/api/roles';
import { canAccess } from '@/lib/auth/canAccess';
import { useAuth } from '@/lib/auth/useAuth';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Building2, Pencil, Plus, Trash2 } from 'lucide-react';
import Link from 'next/link';

export default function RolesPage() {
	const { user: currentUser, selectedTenantId } = useAuth();
	const queryClient = useQueryClient();

	// Super admins need to select a tenant to view tenant-scoped resources
	const needsTenantSelection = currentUser?.isSuperAdmin && !selectedTenantId;

	const { data: roles, isLoading } = useQuery({
		queryKey: ['roles', selectedTenantId],
		queryFn: rolesApi.getAll,
		enabled: canAccess(currentUser, 'roles.view') && !needsTenantSelection,
	});

	const deleteRoleMutation = useMutation({
		mutationFn: rolesApi.delete,
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ['roles'] });
		},
	});

	const handleDeleteRole = (roleId: number, roleName: string) => {
		if (confirm(`Are you sure you want to delete "${roleName}"?`)) {
			deleteRoleMutation.mutate(roleId);
		}
	};

	if (!currentUser || !canAccess(currentUser, 'roles.view')) {
		return (
			<div className="py-12 text-center">
				<h1 className="text-2xl font-bold text-destructive">Access Denied</h1>
				<p className="mt-4 text-muted-foreground">You don&apos;t have permission to view roles.</p>
			</div>
		);
	}

	if (needsTenantSelection) {
		return (
			<div className="py-12 text-center">
				<Building2 className="mx-auto h-12 w-12 text-muted-foreground" />
				<h1 className="mt-4 text-2xl font-bold">Select a Tenant</h1>
				<p className="mt-2 text-muted-foreground">
					Please select a tenant from the header to view and manage roles.
				</p>
			</div>
		);
	}

	return (
		<div className="space-y-6">
			<div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
				<div>
					<h1 className="text-2xl font-bold tracking-tight">Roles</h1>
					<p className="text-muted-foreground">Manage roles and their permissions</p>
				</div>
				{canAccess(currentUser, 'roles.manage') && (
					<Button asChild>
						<Link href="/roles/new">
							<Plus className="mr-2 h-4 w-4" />
							Create Role
						</Link>
					</Button>
				)}
			</div>

			<Card>
				<CardHeader>
					<CardTitle className="text-lg">All Roles</CardTitle>
					<CardDescription>
						{isLoading ? 'Loading...' : `${roles?.length || 0} roles total`}
					</CardDescription>
				</CardHeader>
				<CardContent>
					{isLoading ? (
						<div className="space-y-4">
							{[...Array(3)].map((_, i) => (
								<div key={i} className="flex items-center justify-between">
									<div className="space-y-2">
										<Skeleton className="h-4 w-32" />
										<Skeleton className="h-3 w-48" />
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
										<th className="pb-3 pr-4">Name</th>
										<th className="pb-3 pr-4">Description</th>
										<th className="pb-3 pr-4">Permissions</th>
										<th className="pb-3 text-right">Actions</th>
									</tr>
								</thead>
								<tbody className="divide-y">
									{roles?.map((role) => (
										<tr key={role.id} className="text-sm">
											<td className="py-3 pr-4 font-medium">{role.name}</td>
											<td className="py-3 pr-4 text-muted-foreground">{role.description || '-'}</td>
											<td className="py-3 pr-4">
												<div className="flex flex-wrap gap-1">
													{role.permissions.length > 0 ? (
														role.permissions.slice(0, 3).map((permission) => (
															<Badge key={permission} variant="outline">
																{permission}
															</Badge>
														))
													) : (
														<span className="text-muted-foreground">No permissions</span>
													)}
													{role.permissions.length > 3 && (
														<Badge variant="secondary">+{role.permissions.length - 3}</Badge>
													)}
												</div>
											</td>
											<td className="py-3 text-right">
												<div className="flex items-center justify-end gap-2">
													{canAccess(currentUser, 'roles.manage') && (
														<>
															<Button variant="ghost" size="sm" asChild>
																<Link href={`/roles/${role.id}/edit`}>
																	<Pencil className="h-4 w-4" />
																</Link>
															</Button>
															<Button
																variant="ghost"
																size="sm"
																onClick={() => handleDeleteRole(role.id, role.name)}
																disabled={deleteRoleMutation.isPending}
															>
																<Trash2 className="h-4 w-4 text-destructive" />
															</Button>
														</>
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
