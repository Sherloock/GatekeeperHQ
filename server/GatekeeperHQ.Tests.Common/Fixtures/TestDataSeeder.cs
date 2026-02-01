using GatekeeperHQ.Domain.Constants;
using GatekeeperHQ.Domain.Entities;
using GatekeeperHQ.Infrastructure.Data;
using GatekeeperHQ.Tests.Common.Builders;

namespace GatekeeperHQ.Tests.Common.Fixtures;

public class TestDataSeeder
{
	private readonly AppDbContext _context;

	public TestDataSeeder(AppDbContext context)
	{
		_context = context;
	}

	/// <summary>
	/// Seeds basic test data: Super Admin, one Tenant, one Tenant User with Admin role.
	/// Returns the created entities for use in tests.
	/// </summary>
	public async Task<TestData> SeedBasicDataAsync()
	{
		// Create Super Admin
		var superAdmin = UserBuilder.SuperAdmin()
			.WithEmail("superadmin@test.com")
			.WithPassword("SuperAdmin123!")
			.Build();
		_context.Users.Add(superAdmin);

		// Create Tenant
		var tenant = TenantBuilder.Default()
			.WithName("Test Tenant")
			.WithApiKey("test-tenant-api-key")
			.Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		// Create default permissions for tenant
		var permissions = CreateDefaultPermissions(tenant.Id);
		_context.Permissions.AddRange(permissions);
		await _context.SaveChangesAsync();

		// Create Admin role with all permissions
		var adminRole = RoleBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithName("Admin")
			.WithDescription("Administrator role with all permissions")
			.Build();
		_context.Roles.Add(adminRole);
		await _context.SaveChangesAsync();

		// Assign permissions to admin role
		foreach (var permission in permissions)
		{
			_context.RolePermissions.Add(new RolePermission
			{
				RoleId = adminRole.Id,
				PermissionId = permission.Id
			});
		}
		await _context.SaveChangesAsync();

		// Create Tenant User with Admin role
		var tenantUser = UserBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithEmail("admin@testtenant.com")
			.WithPassword("TenantAdmin123!")
			.Build();
		_context.Users.Add(tenantUser);
		await _context.SaveChangesAsync();

		// Assign Admin role to tenant user
		_context.UserRoles.Add(new UserRole
		{
			UserId = tenantUser.Id,
			RoleId = adminRole.Id
		});
		await _context.SaveChangesAsync();

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
	/// Creates a second tenant with its own user for multi-tenancy isolation tests.
	/// </summary>
	public async Task<(Tenant Tenant, User User, Role AdminRole)> SeedSecondTenantAsync()
	{
		var tenant = TenantBuilder.Default()
			.WithName("Second Tenant")
			.WithApiKey("second-tenant-api-key")
			.Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var permissions = CreateDefaultPermissions(tenant.Id);
		_context.Permissions.AddRange(permissions);
		await _context.SaveChangesAsync();

		var adminRole = RoleBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithName("Admin")
			.Build();
		_context.Roles.Add(adminRole);
		await _context.SaveChangesAsync();

		foreach (var permission in permissions)
		{
			_context.RolePermissions.Add(new RolePermission
			{
				RoleId = adminRole.Id,
				PermissionId = permission.Id
			});
		}
		await _context.SaveChangesAsync();

		var user = UserBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithEmail("admin@secondtenant.com")
			.WithPassword("SecondTenant123!")
			.Build();
		_context.Users.Add(user);
		await _context.SaveChangesAsync();

		_context.UserRoles.Add(new UserRole
		{
			UserId = user.Id,
			RoleId = adminRole.Id
		});
		await _context.SaveChangesAsync();

		return (tenant, user, adminRole);
	}

	private static List<Permission> CreateDefaultPermissions(int tenantId)
	{
		return new List<Permission>
		{
			new() { TenantId = tenantId, Key = Permissions.UsersView, Description = "View users" },
			new() { TenantId = tenantId, Key = Permissions.UsersCreate, Description = "Create users" },
			new() { TenantId = tenantId, Key = Permissions.UsersEdit, Description = "Edit users" },
			new() { TenantId = tenantId, Key = Permissions.UsersDelete, Description = "Delete users" },
			new() { TenantId = tenantId, Key = Permissions.RolesView, Description = "View roles" },
			new() { TenantId = tenantId, Key = Permissions.RolesManage, Description = "Manage roles" },
			new() { TenantId = tenantId, Key = Permissions.PermissionsView, Description = "View permissions" },
			new() { TenantId = tenantId, Key = Permissions.PermissionsCreate, Description = "Create permissions" },
			new() { TenantId = tenantId, Key = Permissions.PermissionsManage, Description = "Manage permissions" },
			new() { TenantId = tenantId, Key = Permissions.DashboardAccess, Description = "Access dashboard" },
			new() { TenantId = tenantId, Key = Permissions.SettingsAccess, Description = "Access settings" },
		};
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
