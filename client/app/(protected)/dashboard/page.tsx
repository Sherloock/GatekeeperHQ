'use client';

import { Badge } from '@/components/ui/badge';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { canAccess } from '@/lib/auth/canAccess';
import { useAuth } from '@/lib/auth/useAuth';
import { Key, Shield, User } from 'lucide-react';

export default function DashboardPage() {
	const { user } = useAuth();

	if (!user || !canAccess(user, 'dashboard.access')) {
		return (
			<div className="py-12 text-center">
				<h1 className="text-2xl font-bold text-destructive">Access Denied</h1>
				<p className="mt-4 text-muted-foreground">
					You don&apos;t have permission to access the dashboard.
				</p>
			</div>
		);
	}

	return (
		<div className="space-y-6">
			<div>
				<h1 className="text-2xl font-bold tracking-tight">Dashboard</h1>
				<p className="text-muted-foreground">Welcome back, {user.email}</p>
			</div>

			<div className="grid gap-4 md:grid-cols-2 lg:grid-cols-3">
				<Card>
					<CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
						<CardTitle className="text-sm font-medium">Account</CardTitle>
						<User className="h-4 w-4 text-muted-foreground" />
					</CardHeader>
					<CardContent>
						<div className="truncate text-lg font-semibold">{user.email}</div>
						<p className="text-xs text-muted-foreground">
							{user.isSuperAdmin ? 'Super Administrator' : 'Standard User'}
						</p>
					</CardContent>
				</Card>

				<Card>
					<CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
						<CardTitle className="text-sm font-medium">Roles</CardTitle>
						<Shield className="h-4 w-4 text-muted-foreground" />
					</CardHeader>
					<CardContent>
						<div className="text-2xl font-bold">{user.roles.length}</div>
						<p className="text-xs text-muted-foreground">Assigned roles</p>
					</CardContent>
				</Card>

				<Card>
					<CardHeader className="flex flex-row items-center justify-between space-y-0 pb-2">
						<CardTitle className="text-sm font-medium">Permissions</CardTitle>
						<Key className="h-4 w-4 text-muted-foreground" />
					</CardHeader>
					<CardContent>
						<div className="text-2xl font-bold">{user.permissions.length}</div>
						<p className="text-xs text-muted-foreground">Active permissions</p>
					</CardContent>
				</Card>
			</div>

			<div className="grid gap-4 md:grid-cols-2">
				<Card>
					<CardHeader>
						<CardTitle className="text-lg">Your Roles</CardTitle>
						<CardDescription>Roles assigned to your account</CardDescription>
					</CardHeader>
					<CardContent>
						{user.roles.length > 0 ? (
							<div className="flex flex-wrap gap-2">
								{user.roles.map((role) => (
									<Badge key={role} variant="secondary">
										{role}
									</Badge>
								))}
							</div>
						) : (
							<p className="text-sm text-muted-foreground">No roles assigned</p>
						)}
					</CardContent>
				</Card>

				<Card>
					<CardHeader>
						<CardTitle className="text-lg">Your Permissions</CardTitle>
						<CardDescription>Permissions granted through your roles</CardDescription>
					</CardHeader>
					<CardContent>
						{user.permissions.length > 0 ? (
							<div className="flex flex-wrap gap-2">
								{user.permissions.map((permission) => (
									<Badge key={permission} variant="outline">
										{permission}
									</Badge>
								))}
							</div>
						) : (
							<p className="text-sm text-muted-foreground">No permissions granted</p>
						)}
					</CardContent>
				</Card>
			</div>
		</div>
	);
}
