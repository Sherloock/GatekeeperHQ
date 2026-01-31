'use client';

import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { Skeleton } from '@/components/ui/skeleton';
import { invitationsApi } from '@/lib/api/invitations';
import { rolesApi } from '@/lib/api/roles';
import { tenantsApi } from '@/lib/api/tenants';
import { canAccess } from '@/lib/auth/canAccess';
import { useAuth } from '@/lib/auth/useAuth';
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query';
import { ArrowLeft, Check, Copy, Plus, X } from 'lucide-react';
import Link from 'next/link';
import { useParams } from 'next/navigation';
import { useState } from 'react';

export default function TenantInvitationsPage() {
	const { user: currentUser } = useAuth();
	const params = useParams();
	const queryClient = useQueryClient();
	const tenantId = Number(params.id);

	const [showCreateInvitationForm, setShowCreateInvitationForm] = useState(false);
	const [newInvitationEmail, setNewInvitationEmail] = useState('');
	const [newInvitationRoleId, setNewInvitationRoleId] = useState<number | undefined>(undefined);
	const [copiedInviteLink, setCopiedInviteLink] = useState<number | null>(null);
	const [error, setError] = useState<string | null>(null);

	const { data: tenant, isLoading: isLoadingTenant } = useQuery({
		queryKey: ['tenants', tenantId],
		queryFn: () => tenantsApi.getById(tenantId),
		enabled: !!tenantId,
	});

	const { data: invitations, isLoading: isLoadingInvitations } = useQuery({
		queryKey: ['invitations', tenantId],
		queryFn: () => invitationsApi.getByTenant(tenantId),
		enabled: !!tenantId,
	});

	const { data: roles } = useQuery({
		queryKey: ['roles'],
		queryFn: rolesApi.getAll,
	});

	const createInvitationMutation = useMutation({
		mutationFn: () =>
			invitationsApi.create(tenantId, {
				email: newInvitationEmail,
				roleId: newInvitationRoleId,
			}),
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ['invitations', tenantId] });
			setShowCreateInvitationForm(false);
			setNewInvitationEmail('');
			setNewInvitationRoleId(undefined);
			setError(null);
		},
		onError: (err: Error & { response?: { data?: { message?: string } } }) => {
			setError(err.response?.data?.message || 'Failed to create invitation');
		},
	});

	const revokeInvitationMutation = useMutation({
		mutationFn: invitationsApi.revoke,
		onSuccess: () => {
			queryClient.invalidateQueries({ queryKey: ['invitations', tenantId] });
		},
	});

	const handleCreateInvitation = (e: React.FormEvent) => {
		e.preventDefault();
		setError(null);
		createInvitationMutation.mutate();
	};

	const handleCopyInviteLink = async (invitationId: number, token: string) => {
		const inviteUrl = `${window.location.origin}/invite/${token}`;
		await navigator.clipboard.writeText(inviteUrl);
		setCopiedInviteLink(invitationId);
		setTimeout(() => setCopiedInviteLink(null), 2000);
	};

	const handleRevokeInvitation = (invitationId: number, email: string) => {
		if (confirm(`Are you sure you want to revoke the invitation for "${email}"?`)) {
			revokeInvitationMutation.mutate(invitationId);
		}
	};

	const getStatusBadgeVariant = (
		status: string
	): 'success' | 'warning' | 'destructive' | 'secondary' => {
		switch (status) {
			case 'pending':
				return 'warning';
			case 'accepted':
				return 'success';
			case 'expired':
				return 'destructive';
			default:
				return 'secondary';
		}
	};

	if (
		!currentUser ||
		(!canAccess(currentUser, 'invitations.manage') && !currentUser.isSuperAdmin)
	) {
		return (
			<div className="py-12 text-center">
				<h1 className="text-2xl font-bold text-destructive">Access Denied</h1>
				<p className="mt-4 text-muted-foreground">
					You don&apos;t have permission to manage invitations.
				</p>
			</div>
		);
	}

	if (isLoadingTenant || isLoadingInvitations) {
		return (
			<div className="space-y-6">
				<Skeleton className="h-10 w-24" />
				<Card>
					<CardHeader>
						<Skeleton className="h-6 w-32" />
						<Skeleton className="h-4 w-48" />
					</CardHeader>
					<CardContent className="space-y-4">
						{[...Array(3)].map((_, i) => (
							<Skeleton key={i} className="h-12 w-full" />
						))}
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
		<div className="space-y-6">
			<div className="flex items-center gap-4">
				<Button variant="ghost" size="sm" asChild>
					<Link href="/tenants">
						<ArrowLeft className="mr-2 h-4 w-4" />
						Back
					</Link>
				</Button>
			</div>

			<div className="flex flex-col gap-4 sm:flex-row sm:items-center sm:justify-between">
				<div>
					<h1 className="text-2xl font-bold tracking-tight">Invitations</h1>
					<p className="text-muted-foreground">Manage invitations for {tenant.name}</p>
				</div>
				<Button onClick={() => setShowCreateInvitationForm(true)}>
					<Plus className="mr-2 h-4 w-4" />
					Create Invitation
				</Button>
			</div>

			{showCreateInvitationForm && (
				<Card>
					<CardHeader>
						<CardTitle className="text-lg">Create New Invitation</CardTitle>
						<CardDescription>Send an invitation to join this tenant</CardDescription>
					</CardHeader>
					<CardContent>
						{error && (
							<div className="mb-4 rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">
								{error}
							</div>
						)}
						<form onSubmit={handleCreateInvitation} className="space-y-4">
							<div className="space-y-2">
								<Label htmlFor="email">Email Address</Label>
								<Input
									id="email"
									type="email"
									value={newInvitationEmail}
									onChange={(e) => setNewInvitationEmail(e.target.value)}
									placeholder="user@example.com"
									required
								/>
							</div>
							<div className="space-y-2">
								<Label htmlFor="role">Assign Role (Optional)</Label>
								<select
									id="role"
									value={newInvitationRoleId || ''}
									onChange={(e) =>
										setNewInvitationRoleId(e.target.value ? Number(e.target.value) : undefined)
									}
									className="flex h-10 w-full rounded-md border border-input bg-background px-3 py-2 text-sm ring-offset-background focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-ring focus-visible:ring-offset-2"
								>
									<option value="">No role assigned</option>
									{roles?.map((role) => (
										<option key={role.id} value={role.id}>
											{role.name}
										</option>
									))}
								</select>
							</div>
							<div className="flex gap-4">
								<Button type="submit" disabled={createInvitationMutation.isPending}>
									{createInvitationMutation.isPending ? 'Creating...' : 'Create Invitation'}
								</Button>
								<Button
									type="button"
									variant="outline"
									onClick={() => {
										setShowCreateInvitationForm(false);
										setError(null);
									}}
								>
									Cancel
								</Button>
							</div>
						</form>
					</CardContent>
				</Card>
			)}

			<Card>
				<CardHeader>
					<CardTitle className="text-lg">All Invitations</CardTitle>
					<CardDescription>{invitations?.length || 0} invitations total</CardDescription>
				</CardHeader>
				<CardContent>
					{invitations?.length === 0 ? (
						<div className="py-8 text-center text-muted-foreground">
							No invitations yet. Create one to invite users to this tenant.
						</div>
					) : (
						<div className="overflow-x-auto">
							<table className="w-full">
								<thead>
									<tr className="border-b text-left text-sm font-medium text-muted-foreground">
										<th className="pb-3 pr-4">Email</th>
										<th className="pb-3 pr-4">Role</th>
										<th className="pb-3 pr-4">Status</th>
										<th className="pb-3 pr-4">Expires</th>
										<th className="pb-3 text-right">Actions</th>
									</tr>
								</thead>
								<tbody className="divide-y">
									{invitations?.map((invitation) => (
										<tr key={invitation.id} className="text-sm">
											<td className="py-3 pr-4 font-medium">{invitation.email}</td>
											<td className="py-3 pr-4 text-muted-foreground">
												{invitation.roleName || 'None'}
											</td>
											<td className="py-3 pr-4">
												<Badge variant={getStatusBadgeVariant(invitation.status)}>
													{invitation.status}
												</Badge>
											</td>
											<td className="py-3 pr-4 text-muted-foreground">
												{new Date(invitation.expiresAt).toLocaleDateString()}
											</td>
											<td className="py-3 text-right">
												{invitation.status === 'pending' && (
													<div className="flex items-center justify-end gap-2">
														<Button
															variant="ghost"
															size="sm"
															onClick={() => handleCopyInviteLink(invitation.id, invitation.token)}
														>
															{copiedInviteLink === invitation.id ? (
																<Check className="h-4 w-4 text-primary" />
															) : (
																<Copy className="h-4 w-4" />
															)}
														</Button>
														<Button
															variant="ghost"
															size="sm"
															onClick={() =>
																handleRevokeInvitation(invitation.id, invitation.email)
															}
															disabled={revokeInvitationMutation.isPending}
														>
															<X className="h-4 w-4 text-destructive" />
														</Button>
													</div>
												)}
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
