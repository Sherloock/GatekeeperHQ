using System.Security.Claims;
using GatekeeperHQ.Application.Services;
using GatekeeperHQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GatekeeperHQ.API.Middleware;

public class TenantResolutionMiddleware
{
	private readonly RequestDelegate _next;
	private readonly ILogger<TenantResolutionMiddleware> _logger;

	public TenantResolutionMiddleware(RequestDelegate next, ILogger<TenantResolutionMiddleware> logger)
	{
		_next = next;
		_logger = logger;
	}

	public async Task InvokeAsync(
		HttpContext context,
		ITenantContext tenantContext,
		AppDbContext dbContext)
	{
		var path = context.Request.Path.Value?.ToLower() ?? string.Empty;

		// Skip tenant resolution for paths that don't require it
		if (ShouldSkipTenantResolution(path, context.Request.Method))
		{
			await _next(context);
			return;
		}

		// If tenant is already set, validate it for authenticated users
		if (tenantContext.TenantId.HasValue)
		{
			// For authenticated non-super-admin users, ensure their tenant context matches their JWT
			// This prevents X-Tenant-ID header spoofing set before authentication
			if (context.User.Identity?.IsAuthenticated == true)
			{
				var isSuperAdminCheck = context.User.FindFirst("is_super_admin")?.Value == "true";
				if (!isSuperAdminCheck)
				{
					var jwtTenantIdClaim = context.User.FindFirst("tenant_id");
					if (jwtTenantIdClaim != null && int.TryParse(jwtTenantIdClaim.Value, out var jwtTenant))
					{
						// Non-super-admin user must use their JWT tenant
						if (tenantContext.TenantId.Value != jwtTenant)
						{
							_logger.LogWarning(
								"Tenant context mismatch: Context had {ContextTenantId} but JWT has {JwtTenantId}. Correcting to JWT tenant.",
								tenantContext.TenantId.Value, jwtTenant);
							tenantContext.SetTenant(jwtTenant);
						}
					}
				}
			}
			await _next(context);
			return;
		}

		// Try to resolve tenant from multiple sources (priority order)
		int? tenantId = null;

		// Check if user is authenticated and if they're a super admin
		var isAuthenticated = context.User.Identity?.IsAuthenticated == true;
		var isSuperAdmin = isAuthenticated && context.User.FindFirst("is_super_admin")?.Value == "true";
		int? jwtTenantId = null;

		// Get tenant ID from JWT claim if authenticated
		if (isAuthenticated)
		{
			var tenantIdClaim = context.User.FindFirst("tenant_id");
			if (tenantIdClaim != null && int.TryParse(tenantIdClaim.Value, out var claimTenantId))
			{
				jwtTenantId = claimTenantId;
			}
		}

		// 1. Try from subdomain (e.g., tenant1.gatekeeperhq.com)
		var host = context.Request.Host.Host;
		if (host.Contains('.'))
		{
			var subdomain = host.Split('.')[0];
			if (!string.IsNullOrEmpty(subdomain) && subdomain != "localhost" && subdomain != "www")
			{
				var tenant = await dbContext.Tenants
					.FirstOrDefaultAsync(t => t.Name.ToLower() == subdomain.ToLower() && t.IsActive);
				if (tenant != null)
				{
					tenantId = tenant.Id;
				}
			}
		}

		// 2. Try from X-Tenant-Id header (for super admin tenant selection ONLY)
		if (tenantId == null && context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader))
		{
			if (int.TryParse(tenantIdHeader.ToString(), out var headerTenantId))
			{
				// SECURITY: Only allow X-Tenant-Id header override for super admins
				// Non-super-admin users must use their JWT tenant claim
				if (isAuthenticated)
				{
					if (isSuperAdmin)
					{
						// Super admins can switch to any tenant
						var tenantExists = await dbContext.Tenants
							.AnyAsync(t => t.Id == headerTenantId && t.IsActive);
						if (tenantExists)
						{
							tenantId = headerTenantId;
						}
					}
					else if (jwtTenantId.HasValue && jwtTenantId.Value == headerTenantId)
					{
						// Non-super-admin: Header must match JWT claim
						tenantId = headerTenantId;
					}
					// If header doesn't match JWT claim for non-super-admin, ignore the header
				}
				else
				{
					// Not authenticated yet - allow header for login flow
					var tenantExists = await dbContext.Tenants
						.AnyAsync(t => t.Id == headerTenantId && t.IsActive);
					if (tenantExists)
					{
						tenantId = headerTenantId;
					}
				}
			}
		}

		// 3. Try from API key header (X-API-Key)
		// Note: This is also handled by ApiKeyAuthenticationMiddleware, but we check here too
		// in case the middleware runs before ApiKeyAuthenticationMiddleware
		if (tenantId == null && context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
		{
			var apiKey = apiKeyHeader.ToString();
			if (!string.IsNullOrEmpty(apiKey))
			{
				var tenant = await dbContext.Tenants
					.FirstOrDefaultAsync(t => t.ApiKey == apiKey && t.IsActive);
				if (tenant != null)
				{
					tenantId = tenant.Id;
				}
			}
		}

		// 4. Try from query parameter (for development/testing)
		// This must work before authentication for login endpoint
		if (tenantId == null && context.Request.Query.TryGetValue("tenantId", out var tenantIdQuery))
		{
			if (int.TryParse(tenantIdQuery.ToString(), out var queryTenantId))
			{
				tenantId = queryTenantId;
			}
		}

		// 5. Try from JWT claim (for authenticated users)
		// Use the cached JWT tenant ID
		if (tenantId == null && jwtTenantId.HasValue)
		{
			tenantId = jwtTenantId.Value;
		}

		// Set tenant context if found
		if (tenantId.HasValue)
		{
			tenantContext.SetTenant(tenantId.Value);
			_logger.LogDebug("Tenant resolved: {TenantId} for {Path}", tenantId.Value, path);
		}
		else
		{
			// Log as debug instead of warning - tenant resolution may be optional for some endpoints
			// (e.g., login endpoint might accept tenantId as query parameter)
			_logger.LogDebug("No tenant could be resolved for request to {Path}", path);
		}

		await _next(context);
	}

	private static bool ShouldSkipTenantResolution(string path, string method)
	{
		// Skip for OPTIONS requests (CORS preflight)
		if (method.Equals("OPTIONS", StringComparison.OrdinalIgnoreCase))
		{
			return true;
		}

		// Skip for health check endpoints
		if (path.StartsWith("/health"))
		{
			return true;
		}

		// Skip for Swagger endpoints
		if (path.StartsWith("/swagger"))
		{
			return true;
		}

		return false;
	}
}
