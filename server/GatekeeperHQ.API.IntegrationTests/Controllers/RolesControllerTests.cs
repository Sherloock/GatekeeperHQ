using System.Net.Http.Json;
using GatekeeperHQ.Tests.Common.Extensions;

namespace GatekeeperHQ.API.IntegrationTests.Controllers;

public class RolesControllerTests : IntegrationTestBase
{
	public RolesControllerTests(CustomWebApplicationFactory factory) : base(factory)
	{
	}

	[Fact]
	public async Task GetAll_WithValidPermission_ReturnsRoles()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Act
		var response = await Client.GetAsync("/api/v1/roles");

		// Assert
		response.ShouldBeOk();
		var result = await response.ReadAsJsonAsync<List<RoleDto>>();
		result.Should().NotBeNull();
		result.Should().HaveCountGreaterThan(0);
		result!.Select(r => r.Name).Should().Contain("Admin");
	}

	[Fact]
	public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
	{
		// Arrange
		ClearAuthentication();

		// Act
		var response = await Client.GetAsync("/api/v1/roles");

		// Assert
		response.ShouldBeUnauthorized();
	}

	[Fact]
	public async Task GetById_WithValidId_ReturnsRole()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Act
		var response = await Client.GetAsync($"/api/v1/roles/{testData.AdminRole.Id}");

		// Assert
		response.ShouldBeOk();
		var result = await response.ReadAsJsonAsync<RoleDto>();
		result.Should().NotBeNull();
		result!.Name.Should().Be("Admin");
		result.Permissions.Should().NotBeEmpty();
	}

	[Fact]
	public async Task Create_WithValidData_CreatesRole()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		var createRequest = new
		{
			Name = "Editor",
			Description = "Can edit content",
			PermissionIds = new[] { testData.Permissions.First().Id }
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/roles", createRequest);

		// Assert
		response.ShouldBeOk();
		var result = await response.ReadAsJsonAsync<RoleDto>();
		result.Should().NotBeNull();
		result!.Name.Should().Be("Editor");
		result.Description.Should().Be("Can edit content");
	}

	[Fact]
	public async Task Create_WithDuplicateName_ReturnsBadRequest()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		var createRequest = new
		{
			Name = "Admin",  // Already exists
			Description = "Duplicate"
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/roles", createRequest);

		// Assert
		response.ShouldBeBadRequest();
	}

	[Fact]
	public async Task Update_WithValidData_UpdatesRole()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Create a role to update
		var createRequest = new
		{
			Name = "ToUpdate",
			Description = "Original description"
		};
		var createResponse = await Client.PostAsJsonAsync("/api/v1/roles", createRequest);
		var createdRole = await createResponse.ReadAsJsonAsync<RoleDto>();

		var updateRequest = new
		{
			Name = "Updated",
			Description = "New description"
		};

		// Act
		var response = await Client.PutAsJsonAsync($"/api/v1/roles/{createdRole!.Id}", updateRequest);

		// Assert
		response.ShouldBeOk();
		var result = await response.ReadAsJsonAsync<RoleDto>();
		result.Should().NotBeNull();
		result!.Name.Should().Be("Updated");
		result.Description.Should().Be("New description");
	}

	[Fact]
	public async Task Delete_WithValidId_DeletesRole()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Create a role to delete
		var createRequest = new
		{
			Name = "ToDelete",
			Description = "Will be deleted"
		};
		var createResponse = await Client.PostAsJsonAsync("/api/v1/roles", createRequest);
		var createdRole = await createResponse.ReadAsJsonAsync<RoleDto>();

		// Act
		var response = await Client.DeleteAsync($"/api/v1/roles/{createdRole!.Id}");

		// Assert
		response.ShouldBeNoContent();
	}

	[Fact]
	public async Task GetRolePermissions_ReturnsPermissions()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Act
		var response = await Client.GetAsync($"/api/v1/roles/{testData.AdminRole.Id}/permissions");

		// Assert
		response.ShouldBeOk();
		var result = await response.ReadAsJsonAsync<List<PermissionDto>>();
		result.Should().NotBeNull();
		result.Should().NotBeEmpty();
	}

	[Fact]
	public async Task AddPermissionToRole_AddsPermission()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Create a role without permissions
		var createRequest = new { Name = "NoPerms" };
		var createResponse = await Client.PostAsJsonAsync("/api/v1/roles", createRequest);
		var createdRole = await createResponse.ReadAsJsonAsync<RoleDto>();

		var addPermRequest = new { PermissionId = testData.Permissions.First().Id };

		// Act
		var response = await Client.PostAsJsonAsync($"/api/v1/roles/{createdRole!.Id}/permissions", addPermRequest);

		// Assert
		response.ShouldBeOk();

		// Verify permission was added
		var getPermsResponse = await Client.GetAsync($"/api/v1/roles/{createdRole.Id}/permissions");
		var perms = await getPermsResponse.ReadAsJsonAsync<List<PermissionDto>>();
		perms.Should().HaveCount(1);
	}

	[Fact]
	public async Task RemovePermissionFromRole_RemovesPermission()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Create a role with permission
		var createRequest = new
		{
			Name = "WithPerm",
			PermissionIds = new[] { testData.Permissions.First().Id }
		};
		var createResponse = await Client.PostAsJsonAsync("/api/v1/roles", createRequest);
		var createdRole = await createResponse.ReadAsJsonAsync<RoleDto>();

		// Act
		var response = await Client.DeleteAsync(
			$"/api/v1/roles/{createdRole!.Id}/permissions/{testData.Permissions.First().Id}");

		// Assert
		response.ShouldBeNoContent();

		// Verify permission was removed
		var getPermsResponse = await Client.GetAsync($"/api/v1/roles/{createdRole.Id}/permissions");
		var perms = await getPermsResponse.ReadAsJsonAsync<List<PermissionDto>>();
		perms.Should().BeEmpty();
	}

	private class RoleDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string? Description { get; set; }
		public DateTime CreatedAt { get; set; }
		public List<string> Permissions { get; set; } = new();
	}

	private class PermissionDto
	{
		public int Id { get; set; }
		public string Key { get; set; } = string.Empty;
		public string? Description { get; set; }
	}
}
