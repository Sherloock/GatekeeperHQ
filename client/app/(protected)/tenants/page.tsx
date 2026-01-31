'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Skeleton } from '@/components/ui/skeleton';
import { tenantsApi } from '@/lib/api/tenants';
import { canAccess } from '@/lib/auth/canAccess';
import { useAuth } from '@/lib/auth/useAuth';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { Check, Copy, Mail, Pencil, Plus, RefreshCw, Trash2 } from 'lucide-react';
import Link from 'next/link';
import { useState } from 'react';

export default function TenantsPage() {
	const { user: currentUser } = useAuth();
	const queryClient = useQueryClient();
	const [copiedApiKey, setCopiedApiKey] = useState<number | null>(null);

	const { data: tenants, isLoading } = useQuery({
		queryKey: ['tenants'],
		queryFn: tenantsApi.getAll,
		enabled: canAccess(currentUser, 'tenants.view') || currentUser?.isSuperAdmin,
	});

	const deleteTenantMutation = useMutation({
		mutationFn: tenantsApi.delete,
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ['tenants'] });
		},
	});

	const regenerateApiKeyMutation = useMutation({
		mutationFn: tenantsApi.regenerateApiKey,
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ['tenants'] });
		},
	});

	const handleCopyApiKey = async (tenantId: number, apiKey: string) => {
		await navigator.clipboard.writeText(apiKey);
		setCopiedApiKey(tenantId);
		setTimeout(() => setCopiedApiKey(null), 2000);
	};

	const handleRegenerateApiKey = (tenantId: number, tenantName: string) => {
		if (
			confirm(
				`Are you sure you want to regenerate the API key for "${tenantName}"? This will invalidate the current key.`
			)
		) {
			regenerateApiKeyMutation.mutate(tenantId);
		}
	};

	const handleDeleteTenant = (tenantId: number, tenantName: string) => {
		if (
			confirm(
				`Are you sure you want to delete "${tenantName}"? This will delete all users, roles, and data associated with this tenant.`
			)
		) {
			deleteTenantMutation.mutate(tenantId);
		}
	};

	if (!currentUser || (!canAccess(currentUser, 'tenants.view') && !currentUser.isSuperAdmin)) {
		return (
			<div className="py-12 text-center">
				<h1 className="text-2xl font-bold text-destructive">Access Denied</h1>
				<p className="mt-4 text-muted-foreground">
					You don&apos;t have permission to view tenants.
				</p>
			</div>
		);
	}

	return (
		<div className="space-y-6">
			<div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
				<div>
					<h1 className="text-2xl font-bold tracking-tight">Tenants</h1>
					<p className="text-muted-foreground">Manage tenant organizations</p>
				</div>
				{(canAccess(currentUser, 'tenants.create') || currentUser.isSuperAdmin) && (
					<Button asChild>
						<Link href="/tenants/new">
							<Plus className="mr-2 h-4 w-4" />
							Create Tenant
						</Link>
					</Button>
				)}
			</div>

			<Card>
				<CardHeader>
					<CardTitle className="text-lg">All Tenants</CardTitle>
					<CardDescription>
						{isLoading ? 'Loading...' : `${tenants?.length || 0} tenants total`}
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
										<th className="pb-3 pr-4">Status</th>
										<th className="pb-3 pr-4">API Key</th>
										<th className="pb-3 pr-4">Created</th>
										<th className="pb-3 text-right">Actions</th>
									</tr>
								</thead>
								<tbody className="divide-y">
									{tenants?.map((tenant) => (
										<tr key={tenant.id} className="text-sm">
											<td className="py-3 pr-4 font-medium">{tenant.name}</td>
											<td className="py-3 pr-4">
												<Badge variant={tenant.isActive ? 'success' : 'destructive'}>
													{tenant.isActive ? 'Active' : 'Inactive'}
												</Badge>
											</td>
											<td className="py-3 pr-4">
												<div className="flex items-center gap-2">
													<code className="rounded bg-muted px-2 py-1 font-mono text-xs">
														{tenant.apiKey ? `${tenant.apiKey.slice(0, 8)}...` : 'N/A'}
													</code>
													{tenant.apiKey && (
														<Button
															variant="ghost"
															size="sm"
															className="h-7 w-7 p-0"
															onClick={() => handleCopyApiKey(tenant.id, tenant.apiKey!)}
														>
															{copiedApiKey === tenant.id ? (
																<Check className="h-3 w-3 text-primary" />
															) : (
																<Copy className="h-3 w-3" />
															)}
														</Button>
													)}
												</div>
											</td>
											<td className="py-3 pr-4 text-muted-foreground">
												{new Date(tenant.createdAt).toLocaleDateString()}
											</td>
											<td className="py-3 text-right">
												<div className="flex items-center justify-end gap-1">
													{(canAccess(currentUser, 'invitations.manage') ||
														currentUser.isSuperAdmin) && (
														<Button variant="ghost" size="sm" asChild>
															<Link href={`/tenants/${tenant.id}/invitations`}>
																<Mail className="h-4 w-4" />
															</Link>
														</Button>
													)}
													{(canAccess(currentUser, 'tenants.manage') ||
														currentUser.isSuperAdmin) && (
														<>
															<Button variant="ghost" size="sm" asChild>
																<Link href={`/tenants/${tenant.id}/edit`}>
																	<Pencil className="h-4 w-4" />
																</Link>
															</Button>
															<Button
																variant="ghost"
																size="sm"
																onClick={() => handleRegenerateApiKey(tenant.id, tenant.name)}
																disabled={regenerateApiKeyMutation.isPending}
															>
																<RefreshCw className="h-4 w-4" />
															</Button>
															<Button
																variant="ghost"
																size="sm"
																onClick={() => handleDeleteTenant(tenant.id, tenant.name)}
																disabled={deleteTenantMutation.isPending}
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
