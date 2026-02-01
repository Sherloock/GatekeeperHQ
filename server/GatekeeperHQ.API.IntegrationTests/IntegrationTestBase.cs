using System.Net.Http.Headers;
using System.Text.Json;
using GatekeeperHQ.Application.Services;
using GatekeeperHQ.Domain.Entities;
using GatekeeperHQ.Infrastructure.Auth;
using GatekeeperHQ.Infrastructure.Data;
using GatekeeperHQ.Tests.Common.Builders;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Respawn;
using Xunit;

namespace GatekeeperHQ.API.IntegrationTests;

[Trait("Category", "Integration")]
public abstract class IntegrationTestBase : IClassFixture<CustomWebApplicationFactory>, IAsyncLifetime
{
	protected readonly CustomWebApplicationFactory Factory;
	protected readonly HttpClient Client;
	private Respawner? _respawner;

	protected static readonly JsonSerializerOptions JsonOptions = new()
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		PropertyNameCaseInsensitive = true
	};

	protected IntegrationTestBase(CustomWebApplicationFactory factory)
	{
		Factory = factory;
		Client = factory.CreateClient();
	}

	public async Task InitializeAsync()
	{
		// Initialize Respawner for database cleanup
		await using var conn = new NpgsqlConnection(Factory.GetConnectionString());
		await conn.OpenAsync();
		
		_respawner = await Respawner.CreateAsync(conn, new RespawnerOptions
		{
			DbAdapter = DbAdapter.Postgres,
			SchemasToInclude = new[] { "public" }
		});

		// Ensure database is migrated
		using var scope = Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		await context.Database.EnsureCreatedAsync();
	}

	public async Task DisposeAsync()
	{
		// Reset database after each test class
		if (_respawner != null)
		{
			await using var conn = new NpgsqlConnection(Factory.GetConnectionString());
			await conn.OpenAsync();
			await _respawner.ResetAsync(conn);
		}
	}

	/// <summary>
	/// Resets the database to a clean state.
	/// Call this at the beginning of each test for complete isolation.
	/// </summary>
	protected async Task ResetDatabaseAsync()
	{
		if (_respawner != null)
		{
			await using var conn = new NpgsqlConnection(Factory.GetConnectionString());
			await conn.OpenAsync();
			await _respawner.ResetAsync(conn);
		}
	}

	/// <summary>
	/// Seeds test data and returns essential entities.
	/// </summary>
	protected async Task<TestData> SeedTestDataAsync()
	{
		using var scope = Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		
		// Create Super Admin
		var superAdmin = UserBuilder.SuperAdmin()
			.WithEmail("superadmin@test.com")
			.WithPassword("SuperAdmin123!")
			.Build();
		context.Users.Add(superAdmin);

		// Create Tenant
		var tenant = TenantBuilder.Default()
			.WithName("Test Tenant")
			.WithApiKey("test-tenant-api-key")
			.Build();
		context.Tenants.Add(tenant);
		await context.SaveChangesAsync();

		// Create default permissions for tenant
		var permissions = CreateDefaultPermissions(tenant.Id);
		context.Permissions.AddRange(permissions);
		await context.SaveChangesAsync();

		// Create Admin role with all permissions
		var adminRole = RoleBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithName("Admin")
			.WithDescription("Administrator role")
			.Build();
		context.Roles.Add(adminRole);
		await context.SaveChangesAsync();

		// Assign permissions to admin role
		foreach (var permission in permissions)
		{
			context.RolePermissions.Add(new RolePermission
			{
				RoleId = adminRole.Id,
				PermissionId = permission.Id
			});
		}
		await context.SaveChangesAsync();

		// Create Tenant User with Admin role
		var tenantUser = UserBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithEmail("admin@testtenant.com")
			.WithPassword("TenantAdmin123!")
			.Build();
		context.Users.Add(tenantUser);
		await context.SaveChangesAsync();

		// Assign Admin role to tenant user
		context.UserRoles.Add(new UserRole
		{
			UserId = tenantUser.Id,
			RoleId = adminRole.Id
		});
		await context.SaveChangesAsync();

		return new TestData
		{
			SuperAdmin = superAdmin,
			Tenant = tenant,
			TenantUser = tenantUser,
			AdminRole = adminRole,
			Permissions = permissions
		};
	}

	/// <summary>
	/// Creates a second tenant for multi-tenancy isolation tests.
	/// </summary>
	protected async Task<(Tenant Tenant, User User, Role AdminRole)> SeedSecondTenantAsync()
	{
		using var scope = Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

		var tenant = TenantBuilder.Default()
			.WithName("Second Tenant")
			.WithApiKey("second-tenant-api-key")
			.Build();
		context.Tenants.Add(tenant);
		await context.SaveChangesAsync();

		var permissions = CreateDefaultPermissions(tenant.Id);
		context.Permissions.AddRange(permissions);
		await context.SaveChangesAsync();

		var adminRole = RoleBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithName("Admin")
			.Build();
		context.Roles.Add(adminRole);
		await context.SaveChangesAsync();

		foreach (var permission in permissions)
		{
			context.RolePermissions.Add(new RolePermission
			{
				RoleId = adminRole.Id,
				PermissionId = permission.Id
			});
		}
		await context.SaveChangesAsync();

		var user = UserBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithEmail("admin@secondtenant.com")
			.WithPassword("SecondTenant123!")
			.Build();
		context.Users.Add(user);
		await context.SaveChangesAsync();

		context.UserRoles.Add(new UserRole
		{
			UserId = user.Id,
			RoleId = adminRole.Id
		});
		await context.SaveChangesAsync();

		return (tenant, user, adminRole);
	}

	/// <summary>
	/// Generates a JWT token for authentication in tests.
	/// </summary>
	protected string GenerateToken(User user, IEnumerable<string>? permissions = null)
	{
		var jwtService = new JwtService(
			CustomWebApplicationFactory.TestJwtSecretKey,
			CustomWebApplicationFactory.TestJwtIssuer,
			CustomWebApplicationFactory.TestJwtAudience,
			60);

		var perms = permissions ?? (user.IsSuperAdmin
			? GatekeeperHQ.Domain.Constants.Permissions.All
			: Array.Empty<string>());

		return jwtService.GenerateToken(
			user.Id,
			user.Email,
			user.TenantId,
			perms,
			user.IsSuperAdmin);
	}

	/// <summary>
	/// Authenticates the HTTP client with a JWT token.
	/// </summary>
	protected void AuthenticateAs(User user, IEnumerable<string>? permissions = null)
	{
		var token = GenerateToken(user, permissions);
		Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
	}

	/// <summary>
	/// Authenticates the HTTP client as a tenant user with tenant header.
	/// </summary>
	protected void AuthenticateAsTenantUser(User user, int tenantId, IEnumerable<string>? permissions = null)
	{
		var token = GenerateToken(user, permissions);
		Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
		Client.DefaultRequestHeaders.Remove("X-Tenant-ID");
		Client.DefaultRequestHeaders.Add("X-Tenant-ID", tenantId.ToString());
	}

	/// <summary>
	/// Clears the authentication from the HTTP client.
	/// </summary>
	protected void ClearAuthentication()
	{
		Client.DefaultRequestHeaders.Authorization = null;
		Client.DefaultRequestHeaders.Remove("X-Tenant-ID");
	}

	private static List<Permission> CreateDefaultPermissions(int tenantId)
	{
		return Domain.Constants.Permissions.TenantPermissions
			.Select(key => new Permission
			{
				TenantId = tenantId,
				Key = key,
				Description = $"Permission: {key}"
			})
			.ToList();
	}
}

public class TestData
{
	public required User SuperAdmin { get; init; }
	public required Tenant Tenant { get; init; }
	public required User TenantUser { get; init; }
	public required Role AdminRole { get; init; }
	public required List<Permission> Permissions { get; init; }
}
