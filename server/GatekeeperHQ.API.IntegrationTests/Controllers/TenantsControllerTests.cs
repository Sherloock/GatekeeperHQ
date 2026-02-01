using System.Net.Http.Json;
using GatekeeperHQ.Tests.Common.Extensions;

namespace GatekeeperHQ.API.IntegrationTests.Controllers;

public class TenantsControllerTests : IntegrationTestBase
{
	public TenantsControllerTests(CustomWebApplicationFactory factory) : base(factory)
	{
	}

	[Fact]
	public async Task GetAll_AsSuperAdmin_ReturnsTenants()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		AuthenticateAs(testData.SuperAdmin);

		// Act
		var response = await Client.GetAsync("/api/v1/tenants");

		// Assert
		response.ShouldBeOk();
		var result = await response.ReadAsJsonAsync<List<TenantDto>>();
		result.Should().NotBeNull();
		result.Should().HaveCountGreaterThan(0);
	}

	[Fact]
	public async Task GetAll_AsNonSuperAdmin_ReturnsForbidden()
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

	[Fact]
	public async Task GetById_AsSuperAdmin_ReturnsTenant()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		AuthenticateAs(testData.SuperAdmin);

		// Act
		var response = await Client.GetAsync($"/api/v1/tenants/{testData.Tenant.Id}");

		// Assert
		response.ShouldBeOk();
		var result = await response.ReadAsJsonAsync<TenantDto>();
		result.Should().NotBeNull();
		result!.Name.Should().Be("Test Tenant");
	}

	[Fact]
	public async Task Create_AsSuperAdmin_CreatesTenant()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		AuthenticateAs(testData.SuperAdmin);

		var createRequest = new
		{
			Name = "New Tenant",
			IsActive = true
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/tenants", createRequest);

		// Assert
		response.ShouldBeCreated();
		var result = await response.ReadAsJsonAsync<TenantDto>();
		result.Should().NotBeNull();
		result!.Name.Should().Be("New Tenant");
		result.ApiKey.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public async Task Create_WithDuplicateName_ReturnsBadRequest()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		AuthenticateAs(testData.SuperAdmin);

		var createRequest = new
		{
			Name = "Test Tenant",  // Already exists
			IsActive = true
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/tenants", createRequest);

		// Assert
		response.ShouldBeConflict();
	}

	[Fact]
	public async Task Update_AsSuperAdmin_UpdatesTenant()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		AuthenticateAs(testData.SuperAdmin);

		var updateRequest = new
		{
			Name = "Updated Tenant Name",
			IsActive = false
		};

		// Act
		var response = await Client.PutAsJsonAsync($"/api/v1/tenants/{testData.Tenant.Id}", updateRequest);

		// Assert
		response.ShouldBeOk();
		var result = await response.ReadAsJsonAsync<TenantDto>();
		result.Should().NotBeNull();
		result!.Name.Should().Be("Updated Tenant Name");
		result.IsActive.Should().BeFalse();
	}

	[Fact]
	public async Task Delete_AsSuperAdmin_DeletesTenant()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		AuthenticateAs(testData.SuperAdmin);

		// Create a tenant to delete
		var createRequest = new { Name = "ToDelete" };
		var createResponse = await Client.PostAsJsonAsync("/api/v1/tenants", createRequest);
		var createdTenant = await createResponse.ReadAsJsonAsync<TenantDto>();

		// Act
		var response = await Client.DeleteAsync($"/api/v1/tenants/{createdTenant!.Id}");

		// Assert
		response.ShouldBeNoContent();
	}

	[Fact]
	public async Task RegenerateApiKey_AsSuperAdmin_ReturnsNewKey()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		AuthenticateAs(testData.SuperAdmin);

		var originalApiKey = testData.Tenant.ApiKey;

		// Act
		var response = await Client.PostAsync($"/api/v1/tenants/{testData.Tenant.Id}/regenerate-api-key", null);

		// Assert
		response.ShouldBeOk();
		var result = await response.ReadAsJsonAsync<ApiKeyResponse>();
		result.Should().NotBeNull();
		result!.ApiKey.Should().NotBeNullOrEmpty();
		result.ApiKey.Should().NotBe(originalApiKey);
	}

	private class TenantDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string? ApiKey { get; set; }
		public bool IsActive { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime UpdatedAt { get; set; }
	}

	private class ApiKeyResponse
	{
		public string ApiKey { get; set; } = string.Empty;
	}
}
