using System.Net.Http.Json;
using GatekeeperHQ.Tests.Common.Extensions;

namespace GatekeeperHQ.API.IntegrationTests.Security;

public class AuthorizationTests : IntegrationTestBase
{
	public AuthorizationTests(CustomWebApplicationFactory factory) : base(factory)
	{
	}

	#region Protected Endpoints Require Authentication

	[Theory]
	[InlineData("/api/v1/users", "GET")]
	[InlineData("/api/v1/users/1", "GET")]
	[InlineData("/api/v1/roles", "GET")]
	[InlineData("/api/v1/roles/1", "GET")]
	[InlineData("/api/v1/permissions", "GET")]
	[InlineData("/api/v1/tenants", "GET")]
	[InlineData("/api/v1/auth/me", "GET")]
	public async Task ProtectedEndpoints_WithoutToken_ReturnUnauthorized(string endpoint, string method)
	{
		// Arrange
		ClearAuthentication();

		// Act
		var response = method switch
		{
			"GET" => await Client.GetAsync(endpoint),
			"POST" => await Client.PostAsJsonAsync(endpoint, new { }),
			"PUT" => await Client.PutAsJsonAsync(endpoint, new { }),
			"DELETE" => await Client.DeleteAsync(endpoint),
			_ => throw new NotSupportedException()
		};

		// Assert
		response.ShouldBeUnauthorized();
	}

	#endregion

	#region Permission-Based Authorization

	[Fact]
	public async Task UsersEndpoint_WithoutUsersViewPermission_ReturnsForbidden()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		// Authenticate with only roles.view permission (not users.view)
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, new[] { "roles.view" });

		// Act
		var response = await Client.GetAsync("/api/v1/users");

		// Assert
		response.ShouldBeForbidden();
	}

	[Fact]
	public async Task UsersCreate_WithoutUsersCreatePermission_ReturnsForbidden()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		// Authenticate with only users.view permission (not users.create)
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, new[] { "users.view" });

		var createRequest = new
		{
			Email = "newuser@test.com",
			Password = "Password123!",
			IsActive = true
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/users", createRequest);

		// Assert
		response.ShouldBeForbidden();
	}

	[Fact]
	public async Task RolesManage_WithoutRolesManagePermission_ReturnsForbidden()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		// Authenticate with only roles.view permission (not roles.manage)
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, new[] { "roles.view" });

		var createRequest = new
		{
			Name = "NewRole"
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/roles", createRequest);

		// Assert
		response.ShouldBeForbidden();
	}

	[Fact]
	public async Task TenantsEndpoint_AsNonSuperAdmin_ReturnsForbidden()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Act
		var response = await Client.GetAsync("/api/v1/tenants");

		// Assert
		response.ShouldBeForbidden();
	}

	#endregion

	#region Super Admin Authorization

	[Fact]
	public async Task SuperAdmin_BypassesAllPermissionChecks()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		AuthenticateAs(testData.SuperAdmin);

		// Act - Super Admin should be able to access tenants
		var response = await Client.GetAsync("/api/v1/tenants");

		// Assert
		response.ShouldBeOk();
	}

	[Fact]
	public async Task SuperAdmin_CanAccessTenantsView()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		AuthenticateAs(testData.SuperAdmin);

		// Act
		var response = await Client.GetAsync("/api/v1/tenants");

		// Assert
		response.ShouldBeOk();
	}

	[Fact]
	public async Task SuperAdmin_CanCreateTenant()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		AuthenticateAs(testData.SuperAdmin);

		var createRequest = new
		{
			Name = "SuperAdminCreatedTenant",
			IsActive = true
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/tenants", createRequest);

		// Assert
		response.ShouldBeCreated();
	}

	#endregion

	#region Permission Inheritance

	[Fact]
	public async Task User_WithRoleHavingPermission_CanAccessResource()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		// The TenantUser has Admin role which has all permissions
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Act
		var response = await Client.GetAsync("/api/v1/users");

		// Assert
		response.ShouldBeOk();
	}

	#endregion
}
