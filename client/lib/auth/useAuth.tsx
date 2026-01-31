'use client';

import { authApi } from '@/lib/api/auth';
import { apiClient } from '@/lib/api/client';
import type { MeResponse } from '@/types/api';
import { createContext, ReactNode, useContext, useEffect, useState } from 'react';

interface AuthContextType {
	user: MeResponse | null;
	loading: boolean;
	selectedTenantId: number | null;
	login: (email: string, password: string) => Promise<void>;
	logout: () => void;
	refreshUser: () => Promise<void>;
	selectTenant: (tenantId: number) => void;
	clearTenant: () => void;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export function AuthProvider({ children }: { children: ReactNode }) {
	const [user, setUser] = useState<MeResponse | null>(null);
	const [loading, setLoading] = useState(true);
	const [selectedTenantId, setSelectedTenantId] = useState<number | null>(null);

	// Initialize selected tenant from localStorage
	useEffect(() => {
		const storedTenantId = apiClient.getSelectedTenant();
		if (storedTenantId) {
			setSelectedTenantId(parseInt(storedTenantId, 10));
		}
	}, []);

	const refreshUser = async () => {
		// Skip API call if no token exists - user is not logged in
		if (!apiClient.hasToken()) {
			setUser(null);
			setLoading(false);
			return;
		}

		try {
			const userData = await authApi.getMe();
			setUser(userData);
		} catch {
			setUser(null);
		} finally {
			setLoading(false);
		}
	};

	useEffect(() => {
		refreshUser();
	}, []);

	const login = async (email: string, password: string) => {
		const response = await authApi.login({ email, password });
		await refreshUser();
	};

	const logout = () => {
		authApi.logout();
		apiClient.clearSelectedTenant();
		setSelectedTenantId(null);
		setUser(null);
	};

	const selectTenant = (tenantId: number) => {
		apiClient.setSelectedTenant(tenantId);
		setSelectedTenantId(tenantId);
	};

	const clearTenant = () => {
		apiClient.clearSelectedTenant();
		setSelectedTenantId(null);
	};

	return (
		<AuthContext.Provider
			value={{
				user,
				loading,
				selectedTenantId,
				login,
				logout,
				refreshUser,
				selectTenant,
				clearTenant,
			}}
		>
			{children}
		</AuthContext.Provider>
	);
}

export function useAuth() {
	const context = useContext(AuthContext);
	if (context === undefined) {
		throw new Error('useAuth must be used within an AuthProvider');
	}
	return context;
}
