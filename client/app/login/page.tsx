'use client';

import { ThemeToggle } from '@/components/theme-toggle';
import { Button } from '@/components/ui/button';
import { Card, CardContent, CardDescription, CardHeader, CardTitle } from '@/components/ui/card';
import { Input } from '@/components/ui/input';
import { Label } from '@/components/ui/label';
import { useAuth } from '@/lib/auth/useAuth';
import { zodResolver } from '@hookform/resolvers/zod';
import { Shield } from 'lucide-react';
import { useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';
import { useForm } from 'react-hook-form';
import { z } from 'zod';

const loginSchema = z.object({
	email: z.string().email('Invalid email address'),
	password: z.string().min(1, 'Password is required'),
});

type LoginFormData = z.infer<typeof loginSchema>;

export default function LoginPage() {
	const router = useRouter();
	const { login, user, loading } = useAuth();
	const [error, setError] = useState<string | null>(null);
	const [isSubmitting, setIsSubmitting] = useState(false);

	const {
		register,
		handleSubmit,
		formState: { errors },
	} = useForm<LoginFormData>({
		resolver: zodResolver(loginSchema),
	});

	useEffect(() => {
		if (!loading && user) {
			router.push('/dashboard');
		}
	}, [user, loading, router]);

	if (loading || user) {
		return (
			<div className="flex min-h-screen items-center justify-center bg-background">
				<div className="text-center">
					<div className="mx-auto h-8 w-8 animate-spin rounded-full border-2 border-primary border-t-transparent"></div>
					<p className="mt-4 text-sm text-muted-foreground">Loading...</p>
				</div>
			</div>
		);
	}

	const handleLoginSubmit = async (data: LoginFormData) => {
		setError(null);
		setIsSubmitting(true);
		try {
			await login(data.email, data.password);
			router.push('/dashboard');
		} catch (err: unknown) {
			const error = err as { response?: { data?: { message?: string } } };
			setError(error.response?.data?.message || 'Login failed. Please check your credentials.');
		} finally {
			setIsSubmitting(false);
		}
	};

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
						<CardTitle className="text-2xl">GatekeeperHQ</CardTitle>
						<CardDescription>Enter your credentials to sign in</CardDescription>
					</CardHeader>
					<CardContent>
						{error && (
							<div className="mb-4 rounded-md border border-destructive/50 bg-destructive/10 p-3 text-sm text-destructive">
								{error}
							</div>
						)}

						<form onSubmit={handleSubmit(handleLoginSubmit)} className="space-y-4">
							<div className="space-y-2">
								<Label htmlFor="email">Email</Label>
								<Input
									{...register('email')}
									id="email"
									type="email"
									placeholder="admin@gatekeeperhq.com"
									autoComplete="email"
								/>
								{errors.email && <p className="text-sm text-destructive">{errors.email.message}</p>}
							</div>

							<div className="space-y-2">
								<Label htmlFor="password">Password</Label>
								<Input
									{...register('password')}
									id="password"
									type="password"
									placeholder="Enter your password"
									autoComplete="current-password"
								/>
								{errors.password && (
									<p className="text-sm text-destructive">{errors.password.message}</p>
								)}
							</div>

							<Button type="submit" className="w-full" disabled={isSubmitting}>
								{isSubmitting ? 'Signing in...' : 'Sign in'}
							</Button>
						</form>

						<div className="mt-6 rounded-md bg-muted p-3 text-center text-sm">
							<p className="text-muted-foreground">Default credentials:</p>
							<p className="mt-1 font-mono text-xs">superadmin@gatekeeperhq.com</p>
							<p className="font-mono text-xs">SuperAdmin123!</p>
						</div>
					</CardContent>
				</Card>
			</div>
		</div>
	);
}
