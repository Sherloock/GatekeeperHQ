'use client';

import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Checkbox } from '@/components/ui/checkbox';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { tenantsApi } from '@/lib/api/tenants';
import { canAccess } from '@/lib/auth/canAccess';
import { useAuth } from '@/lib/auth/useAuth';
import { useMutation, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft } from 'lucide-react';
import Link from 'next/link';
import { useRouter } from 'next/navigation';
import { useState } from 'react';

export default function NewTenantPage() {
	const { user: currentUser } = useAuth();
	const router = useRouter();
	const queryClient = useQueryClient();

	const [formData, setFormData] = useState({
		name: '',
		isActive: true,
	});
	const [error, setError] = useState<string | null>(null);

	const createTenantMutation = useMutation({
		mutationFn: tenantsApi.create,
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ['tenants'] });
			router.push('/tenants');
		},
		onError: (err: Error & { response?: { data?: { message?: string } } }) => {
			setError(err.response?.data?.message || 'Failed to create tenant');
		},
	});

	const handleSubmitCreateTenant = (e: React.FormEvent) => {
		e.preventDefault();
		setError(null);
		createTenantMutation.mutate(formData);
	};

	if (!currentUser || (!canAccess(currentUser, 'tenants.create') && !currentUser.isSuperAdmin)) {
		return (
			<div className="py-12 text-center">
				<h1 className="text-2xl font-bold text-destructive">Access Denied</h1>
				<p className="mt-4 text-muted-foreground">
					You don&apos;t have permission to create tenants.
				</p>
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
					<CardTitle>Create Tenant</CardTitle>
					<CardDescription>Add a new tenant organization</CardDescription>
				</CardHeader>
				<CardContent>
					{error && (
						<div className="mb-4 rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">
							{error}
						</div>
					)}

					<form onSubmit={handleSubmitCreateTenant} className="space-y-6">
						<div className="space-y-2">
							<Label htmlFor="name">Tenant Name *</Label>
							<Input
								id="name"
								type="text"
								value={formData.name}
								onChange={(e) => setFormData({ ...formData, name: e.target.value })}
								placeholder="e.g., Acme Corp"
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

						<div className="flex gap-4">
							<Button type="submit" disabled={createTenantMutation.isPending}>
								{createTenantMutation.isPending ? 'Creating...' : 'Create Tenant'}
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
