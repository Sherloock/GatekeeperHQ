'use client';

import { TenantSelector } from '@/components/tenant-selector';
import { ThemeToggle } from '@/components/theme-toggle';
import { Badge } from '@/components/ui/badge';
import { Button } from '@/components/ui/button';
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuItem,
	DropdownMenuLabel,
	DropdownMenuSeparator,
	DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { canAccess } from '@/lib/auth/canAccess';
import { useAuth } from '@/lib/auth/useAuth';
import { Building2, LayoutDashboard, LogOut, Menu, Shield, Users, X } from 'lucide-react';
import Link from 'next/link';
import { usePathname, useRouter } from 'next/navigation';
import { useEffect, useState } from 'react';

interface NavLinkProps {
	href: string;
	children: React.ReactNode;
	icon?: React.ReactNode;
	isActive?: boolean;
	onClick?: () => void;
}

function NavLink({ href, children, icon, isActive, onClick }: NavLinkProps) {
	return (
		<Link
			href={href}
			onClick={onClick}
			className={`flex items-center gap-2 rounded-md px-3 py-2 text-sm font-medium transition-colors ${
				isActive
					? 'bg-accent text-accent-foreground'
					: 'text-muted-foreground hover:bg-accent hover:text-accent-foreground'
			}`}
		>
			{icon}
			{children}
		</Link>
	);
}

export default function ProtectedLayout({ children }: { children: React.ReactNode }) {
	const router = useRouter();
	const pathname = usePathname();
	const { user, loading, logout } = useAuth();
	const [isMobileMenuOpen, setIsMobileMenuOpen] = useState(false);

	useEffect(() => {
		if (!loading && !user) {
			router.push('/login');
		}
	}, [user, loading, router]);

	useEffect(() => {
		setIsMobileMenuOpen(false);
	}, [pathname]);

	const handleLogout = () => {
		logout();
		router.push('/login');
	};

	if (loading) {
		return (
			<div className="flex min-h-screen items-center justify-center bg-background">
				<div className="text-center">
					<div className="mx-auto h-8 w-8 animate-spin rounded-full border-2 border-primary border-t-transparent"></div>
					<p className="mt-4 text-sm text-muted-foreground">Loading...</p>
				</div>
			</div>
		);
	}

	if (!user) {
		return null;
	}

	const navItems = [
		{
			href: '/dashboard',
			label: 'Dashboard',
			icon: <LayoutDashboard className="h-4 w-4" />,
			show: canAccess(user, 'dashboard.access'),
		},
		{
			href: '/tenants',
			label: 'Tenants',
			icon: <Building2 className="h-4 w-4" />,
			show: canAccess(user, 'tenants.view') || user.isSuperAdmin,
		},
		{
			href: '/users',
			label: 'Users',
			icon: <Users className="h-4 w-4" />,
			show: canAccess(user, 'users.view'),
		},
		{
			href: '/roles',
			label: 'Roles',
			icon: <Shield className="h-4 w-4" />,
			show: canAccess(user, 'roles.view'),
		},
	].filter((item) => item.show);

	return (
		<div className="min-h-screen bg-background">
			<header className="sticky top-0 z-50 border-b bg-background/95 backdrop-blur supports-[backdrop-filter]:bg-background/60">
				<div className="container mx-auto px-4">
					<div className="flex h-14 items-center justify-between">
						<div className="flex items-center gap-6">
							<Link href="/dashboard" className="flex items-center gap-2">
								<Shield className="h-5 w-5 text-primary" />
								<span className="text-lg font-semibold">GatekeeperHQ</span>
							</Link>

							<nav className="hidden items-center gap-1 md:flex">
								{navItems.map((item) => (
									<NavLink
										key={item.href}
										href={item.href}
										icon={item.icon}
										isActive={pathname === item.href || pathname.startsWith(item.href + '/')}
									>
										{item.label}
									</NavLink>
								))}
							</nav>
						</div>

						<div className="flex items-center gap-2">
							{user.isSuperAdmin && <TenantSelector />}
							<ThemeToggle />

							<DropdownMenu>
								<DropdownMenuTrigger asChild>
									<Button variant="ghost" className="hidden gap-2 md:flex">
										<span className="max-w-[150px] truncate text-sm">{user.email}</span>
										{user.isSuperAdmin && <Badge variant="secondary">Super Admin</Badge>}
									</Button>
								</DropdownMenuTrigger>
								<DropdownMenuContent align="end" className="w-56">
									<DropdownMenuLabel>
										<div className="flex flex-col space-y-1">
											<p className="text-sm font-medium">{user.email}</p>
											{user.isSuperAdmin && (
												<p className="text-xs text-muted-foreground">Super Admin</p>
											)}
										</div>
									</DropdownMenuLabel>
									<DropdownMenuSeparator />
									<DropdownMenuItem onClick={handleLogout} className="text-destructive">
										<LogOut className="mr-2 h-4 w-4" />
										Log out
									</DropdownMenuItem>
								</DropdownMenuContent>
							</DropdownMenu>

							<Button
								variant="ghost"
								size="icon"
								className="md:hidden"
								onClick={() => setIsMobileMenuOpen(!isMobileMenuOpen)}
							>
								{isMobileMenuOpen ? <X className="h-5 w-5" /> : <Menu className="h-5 w-5" />}
							</Button>
						</div>
					</div>
				</div>
			</header>

			{isMobileMenuOpen && (
				<div className="border-b bg-background md:hidden">
					<nav className="container mx-auto flex flex-col gap-1 px-4 py-3">
						{navItems.map((item) => (
							<NavLink
								key={item.href}
								href={item.href}
								icon={item.icon}
								isActive={pathname === item.href || pathname.startsWith(item.href + '/')}
								onClick={() => setIsMobileMenuOpen(false)}
							>
								{item.label}
							</NavLink>
						))}
						<div className="my-2 h-px bg-border" />
						<div className="flex items-center justify-between px-3 py-2">
							<div className="flex flex-col">
								<span className="text-sm font-medium">{user.email}</span>
								{user.isSuperAdmin && (
									<span className="text-xs text-muted-foreground">Super Admin</span>
								)}
							</div>
							<Button variant="ghost" size="sm" onClick={handleLogout} className="text-destructive">
								<LogOut className="mr-2 h-4 w-4" />
								Log out
							</Button>
						</div>
					</nav>
				</div>
			)}

			<main className="container mx-auto px-4 py-6">{children}</main>
		</div>
	);
}
