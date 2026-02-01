using System.Net.Http.Json;
using GatekeeperHQ.Tests.Common.Extensions;

namespace GatekeeperHQ.API.IntegrationTests.Controllers;

public class PermissionsControllerTests : IntegrationTestBase
{
	public PermissionsControllerTests(CustomWebApplicationFactory factory) : base(factory)
	{
	}

	[Fact]
	public async Task GetAll_WithValidPermission_ReturnsPermissions()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Act
		var response = await Client.GetAsync("/api/v1/permissions");

		// Assert
		response.ShouldBeOk();
		var result = await response.ReadAsJsonAsync<List<PermissionDto>>();
		result.Should().NotBeNull();
		result.Should().NotBeEmpty();
	}

	[Fact]
	public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
	{
		// Arrange
		ClearAuthentication();

		// Act
		var response = await Client.GetAsync("/api/v1/permissions");

		// Assert
		response.ShouldBeUnauthorized();
	}

	[Fact]
	public async Task GetById_WithValidId_ReturnsPermission()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		var firstPermission = testData.Permissions.First();

		// Act
		var response = await Client.GetAsync($"/api/v1/permissions/{firstPermission.Id}");

		// Assert
		response.ShouldBeOk();
		var result = await response.ReadAsJsonAsync<PermissionDto>();
		result.Should().NotBeNull();
		result!.Key.Should().Be(firstPermission.Key);
	}

	[Fact]
	public async Task Create_WithValidData_CreatesPermission()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		var createRequest = new
		{
			Key = "custom.permission",
			Description = "A custom permission"
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/permissions", createRequest);

		// Assert
		response.ShouldBeOk();
		var result = await response.ReadAsJsonAsync<PermissionDto>();
		result.Should().NotBeNull();
		result!.Key.Should().Be("custom.permission");
	}

	[Fact]
	public async Task Create_WithInvalidKeyFormat_ReturnsBadRequest()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		var createRequest = new
		{
			Key = "invalidformat",  // Missing dot
			Description = "Invalid key"
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/permissions", createRequest);

		// Assert
		response.ShouldBeBadRequest();
	}

	[Fact]
	public async Task Update_WithValidData_UpdatesPermission()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Create a permission to update
		var createRequest = new
		{
			Key = "update.me",
			Description = "Original"
		};
		var createResponse = await Client.PostAsJsonAsync("/api/v1/permissions", createRequest);
		var createdPerm = await createResponse.ReadAsJsonAsync<PermissionDto>();

		var updateRequest = new
		{
			Key = "updated.permission",
			Description = "Updated description"
		};

		// Act
		var response = await Client.PutAsJsonAsync($"/api/v1/permissions/{createdPerm!.Id}", updateRequest);

		// Assert
		response.ShouldBeOk();
		var result = await response.ReadAsJsonAsync<PermissionDto>();
		result.Should().NotBeNull();
		result!.Key.Should().Be("updated.permission");
		result.Description.Should().Be("Updated description");
	}

	[Fact]
	public async Task Delete_WithValidId_DeletesPermission()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Create a permission to delete (not assigned to any role)
		var createRequest = new
		{
			Key = "delete.me",
			Description = "Will be deleted"
		};
		var createResponse = await Client.PostAsJsonAsync("/api/v1/permissions", createRequest);
		var createdPerm = await createResponse.ReadAsJsonAsync<PermissionDto>();

		// Act
		var response = await Client.DeleteAsync($"/api/v1/permissions/{createdPerm!.Id}");

		// Assert
		response.ShouldBeNoContent();
	}

	[Fact]
	public async Task Delete_WhenPermissionInUse_ReturnsBadRequest()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Try to delete a permission that is assigned to Admin role
		var firstPermission = testData.Permissions.First();

		// Act
		var response = await Client.DeleteAsync($"/api/v1/permissions/{firstPermission.Id}");

		// Assert
		response.ShouldBeBadRequest();
	}

	private class PermissionDto
	{
		public int Id { get; set; }
		public string Key { get; set; } = string.Empty;
		public string? Description { get; set; }
	}
}
