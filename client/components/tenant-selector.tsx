'use client';

import { Button } from '@/components/ui/button';
import {
	DropdownMenu,
	DropdownMenuContent,
	DropdownMenuItem,
	DropdownMenuLabel,
	DropdownMenuSeparator,
	DropdownMenuTrigger,
} from '@/components/ui/dropdown-menu';
import { tenantsApi } from '@/lib/api/tenants';
import { useAuth } from '@/lib/auth/useAuth';
import { useQuery } from '@tanstack/react-query';
import { Building2, ChevronDown, X } from 'lucide-react';

export function TenantSelector() {
	const { user, selectedTenantId, selectTenant, clearTenant } = useAuth();

	const { data: tenants, isLoading } = useQuery({
		queryKey: ['tenants'],
		queryFn: tenantsApi.getAll,
		enabled: user?.isSuperAdmin === true,
	});

	// Only show for super admins
	if (!user?.isSuperAdmin) {
		return null;
	}

	const selectedTenant = tenants?.find((t) => t.id === selectedTenantId);

	return (
		<div className="flex items-center gap-2">
			<DropdownMenu>
				<DropdownMenuTrigger asChild>
					<Button variant="outline" size="sm" className="gap-2">
						<Building2 className="h-4 w-4" />
						{isLoading ? (
							'Loading...'
						) : selectedTenant ? (
							<span className="max-w-[120px] truncate">{selectedTenant.name}</span>
						) : (
							'Select Tenant'
						)}
						<ChevronDown className="h-3 w-3 opacity-50" />
					</Button>
				</DropdownMenuTrigger>
				<DropdownMenuContent align="start" className="w-56">
					<DropdownMenuLabel>Select Tenant Context</DropdownMenuLabel>
					<DropdownMenuSeparator />
					{tenants?.map((tenant) => (
						<DropdownMenuItem
							key={tenant.id}
							onClick={() => selectTenant(tenant.id)}
							className={selectedTenantId === tenant.id ? 'bg-accent' : ''}
						>
							<Building2 className="mr-2 h-4 w-4" />
							<span className="truncate">{tenant.name}</span>
							{!tenant.isActive && (
								<span className="ml-auto text-xs text-muted-foreground">(inactive)</span>
							)}
						</DropdownMenuItem>
					))}
					{(!tenants || tenants.length === 0) && (
						<DropdownMenuItem disabled>No tenants available</DropdownMenuItem>
					)}
				</DropdownMenuContent>
			</DropdownMenu>
			{selectedTenant && (
				<Button
					variant="ghost"
					size="icon"
					className="h-8 w-8"
					onClick={clearTenant}
					title="Clear tenant context"
				>
					<X className="h-4 w-4" />
				</Button>
			)}
		</div>
	);
}
