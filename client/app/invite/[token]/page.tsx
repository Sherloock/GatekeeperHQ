'use client';

import { ThemeToggle } from '@/components/theme-toggle';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { invitationsApi } from '@/lib/api/invitations';
import { useMutation, useQuery } from '@tanstack/react-query';
import { CheckCircle, Info, Shield, XCircle } from 'lucide-react';
import Link from 'next/link';
import { useParams, useRouter } from 'next/navigation';
import { useState } from 'react';

export default function AcceptInvitationPage() {
	const params = useParams();
	const router = useRouter();
	const token = params.token as string;

	const [password, setPassword] = useState('');
	const [confirmPassword, setConfirmPassword] = useState('');
	const [error, setError] = useState<string | null>(null);
	const [acceptSuccess, setAcceptSuccess] = useState(false);

	const { data: validation, isLoading: isValidating } = useQuery({
		queryKey: ['invitation', token, 'validate'],
		queryFn: () => invitationsApi.validate(token),
		enabled: !!token,
	});

	const acceptInvitationMutation = useMutation({
		mutationFn: () => invitationsApi.accept(token, { password }),
		onSuccess: (data) => {
			if (data.success) {
				setAcceptSuccess(true);
			} else {
				setError(data.error || 'Failed to accept invitation');
			}
		},
		onError: (err: Error & { response?: { data?: { message?: string } } }) => {
			setError(err.response?.data?.message || 'Failed to accept invitation');
		},
	});

	const handleSubmitAcceptInvitation = (e: React.FormEvent) => {
		e.preventDefault();
		setError(null);

		if (password !== confirmPassword) {
			setError('Passwords do not match');
			return;
		}

		if (password.length < 6) {
			setError('Password must be at least 6 characters');
			return;
		}

		acceptInvitationMutation.mutate();
	};

	if (isValidating) {
		return (
			<div className="flex min-h-screen flex-col bg-background">
				<div className="absolute right-4 top-4">
					<ThemeToggle />
				</div>
				<div className="flex flex-1 items-center justify-center">
					<div className="text-center">
						<div className="mx-auto h-8 w-8 animate-spin rounded-full border-2 border-primary border-t-transparent"></div>
						<p className="mt-4 text-sm text-muted-foreground">Validating invitation...</p>
					</div>
				</div>
			</div>
		);
	}

	if (!validation?.valid) {
		return (
			<div className="flex min-h-screen flex-col bg-background">
				<div className="absolute right-4 top-4">
					<ThemeToggle />
				</div>
				<div className="flex flex-1 items-center justify-center p-4">
					<Card className="w-full max-w-sm text-center">
						<CardHeader>
							<div className="mx-auto mb-2 flex h-12 w-12 items-center justify-center rounded-full bg-destructive/10">
								<XCircle className="h-6 w-6 text-destructive" />
							</div>
							<CardTitle>Invalid Invitation</CardTitle>
							<CardDescription>
								{validation?.error || 'This invitation is no longer valid.'}
							</CardDescription>
						</CardHeader>
						<CardContent>
							<Button asChild variant="outline" className="w-full">
								<Link href="/login">Go to Login</Link>
							</Button>
						</CardContent>
					</Card>
				</div>
			</div>
		);
	}

	if (acceptSuccess) {
		return (
			<div className="flex min-h-screen flex-col bg-background">
				<div className="absolute right-4 top-4">
					<ThemeToggle />
				</div>
				<div className="flex flex-1 items-center justify-center p-4">
					<Card className="w-full max-w-sm text-center">
						<CardHeader>
							<div className="mx-auto mb-2 flex h-12 w-12 items-center justify-center rounded-full bg-primary/10">
								<CheckCircle className="h-6 w-6 text-primary" />
							</div>
							<CardTitle>Welcome!</CardTitle>
							<CardDescription>
								Your account has been created successfully. You can now log in to{' '}
								<span className="font-semibold">{validation.tenantName}</span>.
							</CardDescription>
						</CardHeader>
						<CardContent>
							<Button className="w-full" onClick={() => router.push('/login')}>
								Go to Login
							</Button>
						</CardContent>
					</Card>
				</div>
			</div>
		);
	}

	return (
		<div className="flex min-h-screen flex-col bg-background">
			<div className="absolute right-4 top-4">
				<ThemeToggle />
			</div>

			<div className="flex flex-1 items-center justify-center p-4">
				<Card className="w-full max-w-sm">
					<CardHeader className="space-y-1 text-center">
						<div className="mx-auto mb-2 flex h-12 w-12 items-center justify-center rounded-full bg-primary/10">
							<Shield className="h-6 w-6 text-primary" />
						</div>
						<CardTitle className="text-2xl">Accept Invitation</CardTitle>
						<CardDescription>
							You&apos;ve been invited to join{' '}
							<span className="font-semibold">{validation.tenantName}</span>
						</CardDescription>
					</CardHeader>
					<CardContent>
						<div className="mb-6 rounded-md border bg-muted/50 p-4">
							<div className="flex items-start gap-3">
								<Info className="mt-0.5 h-4 w-4 text-muted-foreground" />
								<div className="space-y-1 text-sm">
									<p>
										<span className="font-medium">Email:</span> {validation.email}
									</p>
									{validation.roleName && (
										<p>
											<span className="font-medium">Role:</span> {validation.roleName}
										</p>
									)}
									<p>
										<span className="font-medium">Expires:</span>{' '}
										{validation.expiresAt
											? new Date(validation.expiresAt).toLocaleDateString()
											: 'N/A'}
									</p>
								</div>
							</div>
						</div>

						{error && (
							<div className="mb-4 rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">
								{error}
							</div>
						)}

						<form onSubmit={handleSubmitAcceptInvitation} className="space-y-4">
							<div className="space-y-2">
								<Label htmlFor="password">Create Password</Label>
								<Input
									id="password"
									type="password"
									value={password}
									onChange={(e) => setPassword(e.target.value)}
									placeholder="At least 6 characters"
									required
									minLength={6}
								/>
							</div>

							<div className="space-y-2">
								<Label htmlFor="confirmPassword">Confirm Password</Label>
								<Input
									id="confirmPassword"
									type="password"
									value={confirmPassword}
									onChange={(e) => setConfirmPassword(e.target.value)}
									placeholder="Repeat your password"
									required
									minLength={6}
								/>
							</div>

							<Button
								type="submit"
								className="w-full"
								disabled={acceptInvitationMutation.isPending}
							>
								{acceptInvitationMutation.isPending ? 'Creating Account...' : 'Create Account'}
							</Button>
						</form>

						<p className="mt-6 text-center text-sm text-muted-foreground">
							Already have an account?{' '}
							<Link href="/login" className="font-medium text-primary hover:underline">
								Sign in
							</Link>
						</p>
					</CardContent>
				</Card>
			</div>
		</div>
	);
}
