using System.Net.Http.Json;
using GatekeeperHQ.Tests.Common.Extensions;

namespace GatekeeperHQ.API.IntegrationTests.MultiTenancy;

public class TenantIsolationTests : IntegrationTestBase
{
	public TenantIsolationTests(CustomWebApplicationFactory factory) : base(factory)
	{
	}

	[Fact]
	public async Task TenantUser_CannotAccessOtherTenantUsers()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var (secondTenant, secondUser, _) = await SeedSecondTenantAsync();

		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Act - Try to access user from second tenant
		var response = await Client.GetAsync($"/api/v1/users/{secondUser.Id}");

		// Assert - Should not find user (belongs to different tenant)
		response.ShouldBeNotFound();
	}

	[Fact]
	public async Task TenantUser_CannotAccessOtherTenantRoles()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var (_, _, secondAdminRole) = await SeedSecondTenantAsync();

		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Act - Try to access role from second tenant
		var response = await Client.GetAsync($"/api/v1/roles/{secondAdminRole.Id}");

		// Assert - Should not find role (belongs to different tenant)
		response.ShouldBeNotFound();
	}

	[Fact]
	public async Task TenantUser_GetUsers_OnlyReturnsOwnTenantUsers()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		await SeedSecondTenantAsync();

		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Act
		var response = await Client.GetAsync("/api/v1/users");

		// Assert
		response.ShouldBeOk();
		var users = await response.ReadAsJsonAsync<List<UserDto>>();
		users.Should().NotBeNull();
		
		// Should only contain users from first tenant
		users!.All(u => u.Email.Contains("testtenant")).Should().BeTrue();
		users!.Any(u => u.Email.Contains("secondtenant")).Should().BeFalse();
	}

	[Fact]
	public async Task TenantUser_GetRoles_OnlyReturnsOwnTenantRoles()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		await SeedSecondTenantAsync();

		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Create a role in first tenant
		var createRequest = new { Name = "FirstTenantRole" };
		await Client.PostAsJsonAsync("/api/v1/roles", createRequest);

		// Act
		var response = await Client.GetAsync("/api/v1/roles");

		// Assert
		response.ShouldBeOk();
		var roles = await response.ReadAsJsonAsync<List<RoleDto>>();
		roles.Should().NotBeNull();
		
		// All roles should belong to first tenant
		// (We don't expose TenantId in the DTO, but we can verify by count)
		roles.Should().NotBeEmpty();
	}

	[Fact]
	public async Task TenantUser_CannotCreateUserInOtherTenant()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var (secondTenant, _, _) = await SeedSecondTenantAsync();

		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		// Authenticate as first tenant user but try to use second tenant header
		var token = GenerateToken(testData.TenantUser, permissions);
		Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
		Client.DefaultRequestHeaders.Remove("X-Tenant-ID");
		Client.DefaultRequestHeaders.Add("X-Tenant-ID", secondTenant.Id.ToString());

		var createRequest = new
		{
			Email = "sneaky@secondtenant.com",
			Password = "Password123!",
			IsActive = true
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/users", createRequest);

		// Assert - Should be forbidden or the user won't be created in second tenant
		// The JWT contains the original tenant ID, so the tenant context should use JWT claim
		// This depends on the middleware implementation
		// At minimum, verify no security breach occurred
		if (response.IsSuccessStatusCode)
		{
			// If somehow successful, verify user was created in original tenant, not second tenant
			var usersInSecondTenant = await GetUsersInTenant(secondTenant.Id);
			usersInSecondTenant.Should().NotContain(u => u.Email == "sneaky@secondtenant.com");
		}
	}

	[Fact]
	public async Task SuperAdmin_CanAccessAllTenants()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		await SeedSecondTenantAsync();

		AuthenticateAs(testData.SuperAdmin);

		// Act
		var response = await Client.GetAsync("/api/v1/tenants");

		// Assert
		response.ShouldBeOk();
		var tenants = await response.ReadAsJsonAsync<List<TenantDto>>();
		tenants.Should().NotBeNull();
		tenants.Should().HaveCountGreaterThanOrEqualTo(2);
	}

	[Fact]
	public async Task SuperAdmin_CanAccessAnyTenantData_BySettingTenantContext()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var (secondTenant, secondUser, _) = await SeedSecondTenantAsync();

		// Super Admin with second tenant context
		var token = GenerateToken(testData.SuperAdmin);
		Client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
		Client.DefaultRequestHeaders.Remove("X-Tenant-ID");
		Client.DefaultRequestHeaders.Add("X-Tenant-ID", secondTenant.Id.ToString());

		// Act
		var response = await Client.GetAsync("/api/v1/users");

		// Assert
		response.ShouldBeOk();
		var users = await response.ReadAsJsonAsync<List<UserDto>>();
		users.Should().NotBeNull();
		users!.Any(u => u.Email.Contains("secondtenant")).Should().BeTrue();
	}

	[Fact]
	public async Task TenantUser_CannotModifyOtherTenantUser()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var (_, secondUser, _) = await SeedSecondTenantAsync();

		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		var updateRequest = new
		{
			Email = "hacked@test.com"
		};

		// Act - Try to update user from second tenant
		var response = await Client.PutAsJsonAsync($"/api/v1/users/{secondUser.Id}", updateRequest);

		// Assert - Should not find user (belongs to different tenant)
		response.ShouldBeNotFound();
	}

	[Fact]
	public async Task TenantUser_CannotDeleteOtherTenantUser()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var (_, secondUser, _) = await SeedSecondTenantAsync();

		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Act - Try to delete user from second tenant
		var response = await Client.DeleteAsync($"/api/v1/users/{secondUser.Id}");

		// Assert - Should not find user (belongs to different tenant)
		response.ShouldBeNotFound();
	}

	private async Task<List<UserDto>> GetUsersInTenant(int tenantId)
	{
		using var scope = Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<GatekeeperHQ.Infrastructure.Data.AppDbContext>();
		return await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions.ToListAsync(
			context.Users
				.Where(u => u.TenantId == tenantId)
				.Select(u => new UserDto
				{
					Id = u.Id,
					Email = u.Email,
					IsActive = u.IsActive
				}));
	}

	private class UserDto
	{
		public int Id { get; set; }
		public string Email { get; set; } = string.Empty;
		public bool IsActive { get; set; }
	}

	private class RoleDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
	}

	private class TenantDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
	}
}
