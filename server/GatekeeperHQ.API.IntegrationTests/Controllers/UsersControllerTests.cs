using System.Net;
using System.Net.Http.Json;
using GatekeeperHQ.Tests.Common.Extensions;

namespace GatekeeperHQ.API.IntegrationTests.Controllers;

public class UsersControllerTests : IntegrationTestBase
{
	public UsersControllerTests(CustomWebApplicationFactory factory) : base(factory)
	{
	}

	[Fact]
	public async Task GetAll_WithValidPermission_ReturnsUsers()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Act
		var response = await Client.GetAsync("/api/v1/users");

		// Assert
		response.ShouldBeOk();
		var result = await response.ReadAsJsonAsync<List<UserDto>>();
		result.Should().NotBeNull();
		result.Should().HaveCountGreaterThan(0);
	}

	[Fact]
	public async Task GetAll_WithoutPermission_ReturnsForbidden()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		// Authenticate with no permissions
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, Array.Empty<string>());

		// Act
		var response = await Client.GetAsync("/api/v1/users");

		// Assert
		response.ShouldBeForbidden();
	}

	[Fact]
	public async Task GetAll_WithoutAuthentication_ReturnsUnauthorized()
	{
		// Arrange
		ClearAuthentication();

		// Act
		var response = await Client.GetAsync("/api/v1/users");

		// Assert
		response.ShouldBeUnauthorized();
	}

	[Fact]
	public async Task GetById_WithValidId_ReturnsUser()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Act
		var response = await Client.GetAsync($"/api/v1/users/{testData.TenantUser.Id}");

		// Assert
		response.ShouldBeOk();
		var result = await response.ReadAsJsonAsync<UserDto>();
		result.Should().NotBeNull();
		result!.Email.Should().Be("admin@testtenant.com");
	}

	[Fact]
	public async Task GetById_WithInvalidId_ReturnsNotFound()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Act
		var response = await Client.GetAsync("/api/v1/users/99999");

		// Assert
		response.ShouldBeNotFound();
	}

	[Fact]
	public async Task Create_WithValidData_CreatesUser()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		var createRequest = new
		{
			Email = "newuser@testtenant.com",
			Password = "NewUser123!",
			IsActive = true,
			RoleIds = new[] { testData.AdminRole.Id }
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/users", createRequest);

		// Assert
		response.ShouldBeCreated();
		var result = await response.ReadAsJsonAsync<UserDto>();
		result.Should().NotBeNull();
		result!.Email.Should().Be("newuser@testtenant.com");
		result.Roles.Should().Contain("Admin");
	}

	[Fact]
	public async Task Create_WithDuplicateEmail_ReturnsBadRequest()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		var createRequest = new
		{
			Email = "admin@testtenant.com",  // Already exists
			Password = "Password123!",
			IsActive = true
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/users", createRequest);

		// Assert
		response.ShouldBeConflict();
	}

	[Fact]
	public async Task Update_WithValidData_UpdatesUser()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Create a user to update
		var createRequest = new
		{
			Email = "updateme@testtenant.com",
			Password = "Password123!",
			IsActive = true
		};
		var createResponse = await Client.PostAsJsonAsync("/api/v1/users", createRequest);
		var createdUser = await createResponse.ReadAsJsonAsync<UserDto>();

		var updateRequest = new
		{
			Email = "updated@testtenant.com",
			IsActive = false
		};

		// Act
		var response = await Client.PutAsJsonAsync($"/api/v1/users/{createdUser!.Id}", updateRequest);

		// Assert
		response.ShouldBeOk();
		var result = await response.ReadAsJsonAsync<UserDto>();
		result.Should().NotBeNull();
		result!.Email.Should().Be("updated@testtenant.com");
		result.IsActive.Should().BeFalse();
	}

	[Fact]
	public async Task Delete_WithValidId_DeletesUser()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Create a user to delete
		var createRequest = new
		{
			Email = "deleteme@testtenant.com",
			Password = "Password123!",
			IsActive = true
		};
		var createResponse = await Client.PostAsJsonAsync("/api/v1/users", createRequest);
		var createdUser = await createResponse.ReadAsJsonAsync<UserDto>();

		// Act
		var response = await Client.DeleteAsync($"/api/v1/users/{createdUser!.Id}");

		// Assert
		response.ShouldBeNoContent();

		// Verify deletion
		var getResponse = await Client.GetAsync($"/api/v1/users/{createdUser.Id}");
		getResponse.ShouldBeNotFound();
	}

	private class UserDto
	{
		public int Id { get; set; }
		public string Email { get; set; } = string.Empty;
		public bool IsActive { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
		public List<string> Roles { get; set; } = new();
	}
}
