using System.Net.Http.Json;
using GatekeeperHQ.Tests.Common.Extensions;

namespace GatekeeperHQ.API.IntegrationTests.Security;

public class InputValidationTests : IntegrationTestBase
{
	public InputValidationTests(CustomWebApplicationFactory factory) : base(factory)
	{
	}

	#region Email Validation

	[Theory]
	[InlineData("")]
	[InlineData("invalidemail")]
	[InlineData("@nodomain.com")]
	[InlineData("spaces in@email.com")]
	public async Task CreateUser_WithInvalidEmail_ReturnsBadRequest(string invalidEmail)
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		var createRequest = new
		{
			Email = invalidEmail,
			Password = "ValidPassword123!",
			IsActive = true
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/users", createRequest);

		// Assert
		response.ShouldBeBadRequest();
	}

	#endregion

	#region Permission Key Validation

	[Theory]
	[InlineData("")]
	[InlineData("nodot")]
	[InlineData("   ")]
	public async Task CreatePermission_WithInvalidKeyFormat_ReturnsBadRequest(string invalidKey)
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		var createRequest = new
		{
			Key = invalidKey,
			Description = "Test"
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/permissions", createRequest);

		// Assert
		response.ShouldBeBadRequest();
	}

	#endregion

	#region Role Name Validation

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public async Task CreateRole_WithEmptyName_ReturnsBadRequest(string invalidName)
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		var createRequest = new
		{
			Name = invalidName
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/roles", createRequest);

		// Assert
		response.ShouldBeBadRequest();
	}

	#endregion

	#region Tenant Name Validation

	[Theory]
	[InlineData("")]
	[InlineData("   ")]
	public async Task CreateTenant_WithEmptyName_ReturnsBadRequest(string invalidName)
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		AuthenticateAs(testData.SuperAdmin);

		var createRequest = new
		{
			Name = invalidName,
			IsActive = true
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/tenants", createRequest);

		// Assert
		response.ShouldBeBadRequest();
	}

	#endregion

	#region SQL Injection Prevention

	[Fact]
	public async Task Login_WithSqlInjectionAttempt_DoesNotExecuteInjection()
	{
		// Arrange
		await ResetDatabaseAsync();
		await SeedTestDataAsync();

		var loginRequest = new
		{
			Email = "admin@test.com'; DROP TABLE Users; --",
			Password = "password"
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

		// Assert - Should just return 401, not crash or execute SQL
		response.ShouldBeUnauthorized();

		// Verify database is intact
		using var scope = Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<GatekeeperHQ.Infrastructure.Data.AppDbContext>();
		var usersExist = context.Users.Any();
		usersExist.Should().BeTrue("Database should not be affected by SQL injection attempt");
	}

	[Fact]
	public async Task CreateUser_WithSqlInjectionInEmail_DoesNotExecuteInjection()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		var createRequest = new
		{
			Email = "test@test.com'; DELETE FROM Users; --",
			Password = "ValidPassword123!",
			IsActive = true
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/users", createRequest);

		// Assert - Should return bad request (invalid email) or store safely
		// Either way, database should not be affected
		using var scope = Factory.Services.CreateScope();
		var context = scope.ServiceProvider.GetRequiredService<GatekeeperHQ.Infrastructure.Data.AppDbContext>();
		var usersExist = context.Users.Any();
		usersExist.Should().BeTrue("Database should not be affected by SQL injection attempt");
	}

	#endregion

	#region XSS Prevention (Input Sanitization)

	[Fact]
	public async Task CreateRole_WithXssInName_StoredSafely()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		var xssPayload = "<script>alert('xss')</script>";
		var createRequest = new
		{
			Name = $"Role{xssPayload}",
			Description = xssPayload
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/roles", createRequest);

		// Assert - Role might be created, but the payload should be stored as-is (not executed)
		// The frontend should handle escaping when displaying
		if (response.IsSuccessStatusCode)
		{
			var result = await response.ReadAsJsonAsync<RoleDto>();
			result.Should().NotBeNull();
			// The payload is stored as-is (it's the frontend's responsibility to escape)
		}
	}

	#endregion

	#region Long Input Prevention

	[Fact]
	public async Task CreateUser_WithExtremelyLongEmail_ReturnsBadRequest()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		var longEmail = new string('a', 500) + "@test.com";
		var createRequest = new
		{
			Email = longEmail,
			Password = "ValidPassword123!",
			IsActive = true
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/users", createRequest);

		// Assert - Should return bad request due to length constraint
		response.ShouldBeBadRequest();
	}

	#endregion

	private class RoleDto
	{
		public int Id { get; set; }
		public string Name { get; set; } = string.Empty;
		public string? Description { get; set; }
	}
}
