'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import { tenantsApi } from '@/lib/api/tenants';
import { canAccess } from '@/lib/auth/canAccess';
import { useAuth } from '@/lib/auth/useAuth';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft } from 'lucide-react';
import Link from 'next/link';
import { useParams, useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';

export default function EditTenantPage() {
	const { user: currentUser } = useAuth();
	const router = useRouter();
	const params = useParams();
	const queryClient = useQueryClient();
	const tenantId = Number(params.id);

	const [formData, setFormData] = useState({
		name: '',
		isActive: true,
	});
	const [error, setError] = useState<string | null>(null);

	const { data: tenant, isLoading } = useQuery({
		queryKey: ['tenants', tenantId],
		queryFn: () => tenantsApi.getById(tenantId),
		enabled: !!tenantId,
	});

	useEffect(() => {
		if (tenant) {
			setFormData({
				name: tenant.name,
				isActive: tenant.isActive,
			});
		}
	}, [tenant]);

	const updateTenantMutation = useMutation({
		mutationFn: (data: typeof formData) => tenantsApi.update(tenantId, data),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ['tenants'] });
			router.push('/tenants');
		},
		onError: (err: Error & { response?: { data?: { message?: string } } }) => {
			setError(err.response?.data?.message || 'Failed to update tenant');
		},
	});

	const handleSubmitUpdateTenant = (e: React.FormEvent) => {
		e.preventDefault();
		setError(null);
		updateTenantMutation.mutate(formData);
	};

	if (!currentUser || (!canAccess(currentUser, 'tenants.manage') && !currentUser.isSuperAdmin)) {
		return (
			<div className="py-12 text-center">
				<h1 className="text-2xl font-bold text-destructive">Access Denied</h1>
				<p className="mt-4 text-muted-foreground">
					You don&apos;t have permission to edit tenants.
				</p>
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
						<Skeleton className="h-6 w-24" />
						<Skeleton className="h-20 w-full" />
					</CardContent>
				</Card>
			</div>
		);
	}

	if (!tenant) {
		return (
			<div className="py-12 text-center">
				<h1 className="text-2xl font-bold text-destructive">Tenant Not Found</h1>
				<Button variant="link" asChild className="mt-4">
					<Link href="/tenants">Back to Tenants</Link>
				</Button>
			</div>
		);
	}

	return (
		<div className="mx-auto max-w-2xl space-y-6">
			<div className="flex items-center gap-4">
				<Button variant="ghost" size="sm" asChild>
					<Link href="/tenants">
						<ArrowLeft className="mr-2 h-4 w-4" />
						Back
					</Link>
				</Button>
			</div>

			<Card>
				<CardHeader>
					<CardTitle>Edit Tenant</CardTitle>
					<CardDescription>Update tenant information</CardDescription>
				</CardHeader>
				<CardContent>
					{error && (
						<div className="mb-4 rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">
							{error}
						</div>
					)}

					<form onSubmit={handleSubmitUpdateTenant} className="space-y-6">
						<div className="space-y-2">
							<Label htmlFor="name">Tenant Name</Label>
							<Input
								id="name"
								type="text"
								value={formData.name}
								onChange={(e) => setFormData({ ...formData, name: e.target.value })}
								required
							/>
						</div>

						<div className="flex items-center space-x-2">
							<Checkbox
								id="isActive"
								checked={formData.isActive}
								onCheckedChange={(checked) =>
									setFormData({ ...formData, isActive: checked === true })
								}
							/>
							<Label htmlFor="isActive" className="cursor-pointer">
								Active
							</Label>
						</div>

						<div className="space-y-2 rounded-md border p-4">
							<Label>API Key</Label>
							<code className="block rounded bg-muted px-3 py-2 font-mono text-sm">
								{tenant.apiKey || 'N/A'}
							</code>
							<p className="text-xs text-muted-foreground">
								Use the regenerate button on the tenant list to create a new key.
							</p>
						</div>

						<div className="flex gap-4">
							<Button type="submit" disabled={updateTenantMutation.isPending}>
								{updateTenantMutation.isPending ? 'Saving...' : 'Save Changes'}
							</Button>
							<Button type="button" variant="outline" asChild>
								<Link href="/tenants">Cancel</Link>
							</Button>
						</div>
					</form>
				</CardContent>
			</Card>
		</div>
	);
}
