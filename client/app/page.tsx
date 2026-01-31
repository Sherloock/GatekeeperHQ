'use client';

import { useAuth } from '@/lib/auth/useAuth';
import { useRouter } from 'next/navigation';
import { useEffect } from 'react';

export default function Home() {
	const router = useRouter();
	const { user, loading } = useAuth();

	useEffect(() => {
		if (!loading) {
			if (user) {
				router.push('/dashboard');
			} else {
				router.push('/login');
			}
		}
	}, [user, loading, router]);

	return (
		<div className="flex min-h-screen items-center justify-center bg-background">
			<div className="text-center">
				<div className="mx-auto h-8 w-8 animate-spin rounded-full border-2 border-primary border-t-transparent"></div>
				<p className="mt-4 text-sm text-muted-foreground">Loading...</p>
			</div>
		</div>
	);
}
