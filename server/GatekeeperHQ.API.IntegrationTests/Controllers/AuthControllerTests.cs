using System.Net;
using System.Net.Http.Json;
using GatekeeperHQ.Tests.Common.Extensions;

namespace GatekeeperHQ.API.IntegrationTests.Controllers;

public class AuthControllerTests : IntegrationTestBase
{
	public AuthControllerTests(CustomWebApplicationFactory factory) : base(factory)
	{
	}

	#region Login Tests

	[Fact]
	public async Task Login_WithValidSuperAdminCredentials_ReturnsJwtToken()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();

		var loginRequest = new
		{
			Email = "superadmin@test.com",
			Password = "SuperAdmin123!"
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

		// Assert
		response.ShouldBeOk();
		var result = await response.ReadAsJsonAsync<LoginResponse>();
		result.Should().NotBeNull();
		result!.Token.Should().NotBeNullOrEmpty();
		result.RefreshToken.Should().NotBeNullOrEmpty();
		result.IsSuperAdmin.Should().BeTrue();
	}

	[Fact]
	public async Task Login_WithValidTenantUserCredentials_ReturnsJwtToken()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();

		// Add tenant header for tenant user login
		Client.DefaultRequestHeaders.Add("X-Tenant-ID", testData.Tenant.Id.ToString());

		var loginRequest = new
		{
			Email = "admin@testtenant.com",
			Password = "TenantAdmin123!"
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

		// Assert
		response.ShouldBeOk();
		var result = await response.ReadAsJsonAsync<LoginResponse>();
		result.Should().NotBeNull();
		result!.Token.Should().NotBeNullOrEmpty();
		result.RefreshToken.Should().NotBeNullOrEmpty();
		result.IsSuperAdmin.Should().BeFalse();
		result.Permissions.Should().NotBeEmpty();
	}

	[Fact]
	public async Task Login_WithInvalidPassword_ReturnsUnauthorized()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();

		var loginRequest = new
		{
			Email = "superadmin@test.com",
			Password = "WrongPassword123!"
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

		// Assert
		response.ShouldBeUnauthorized();
	}

	[Fact]
	public async Task Login_WithNonExistentEmail_ReturnsUnauthorized()
	{
		// Arrange
		await ResetDatabaseAsync();
		await SeedTestDataAsync();

		var loginRequest = new
		{
			Email = "nonexistent@test.com",
			Password = "Password123!"
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

		// Assert
		response.ShouldBeUnauthorized();
	}

	[Fact]
	public async Task Login_WithInactiveUser_ReturnsUnauthorized()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();

		// Deactivate the user
		using (var scope = Factory.Services.CreateScope())
		{
			var context = scope.ServiceProvider.GetRequiredService<GatekeeperHQ.Infrastructure.Data.AppDbContext>();
			testData.SuperAdmin.IsActive = false;
			context.Users.Update(testData.SuperAdmin);
			await context.SaveChangesAsync();
		}

		var loginRequest = new
		{
			Email = "superadmin@test.com",
			Password = "SuperAdmin123!"
		};

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);

		// Assert
		response.ShouldBeUnauthorized();
	}

	#endregion

	#region Me Endpoint Tests

	[Fact]
	public async Task Me_WithValidToken_ReturnsUserInfo()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		AuthenticateAs(testData.SuperAdmin);

		// Act
		var response = await Client.GetAsync("/api/v1/auth/me");

		// Assert
		response.ShouldBeOk();
		var result = await response.ReadAsJsonAsync<MeResponse>();
		result.Should().NotBeNull();
		result!.Email.Should().Be("superadmin@test.com");
		result.IsSuperAdmin.Should().BeTrue();
	}

	[Fact]
	public async Task Me_WithoutToken_ReturnsUnauthorized()
	{
		// Arrange
		ClearAuthentication();

		// Act
		var response = await Client.GetAsync("/api/v1/auth/me");

		// Assert
		response.ShouldBeUnauthorized();
	}

	[Fact]
	public async Task Me_AsTenantUser_ReturnsUserWithRolesAndPermissions()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();
		
		var permissions = testData.Permissions.Select(p => p.Key).ToList();
		AuthenticateAsTenantUser(testData.TenantUser, testData.Tenant.Id, permissions);

		// Act
		var response = await Client.GetAsync("/api/v1/auth/me");

		// Assert
		response.ShouldBeOk();
		var result = await response.ReadAsJsonAsync<MeResponse>();
		result.Should().NotBeNull();
		result!.Email.Should().Be("admin@testtenant.com");
		result.IsSuperAdmin.Should().BeFalse();
		result.Roles.Should().NotBeEmpty();
		result.Permissions.Should().NotBeEmpty();
	}

	#endregion

	#region Refresh Token Tests

	[Fact]
	public async Task Refresh_WithValidRefreshToken_ReturnsNewTokens()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();

		// First login to get a refresh token
		var loginRequest = new
		{
			Email = "superadmin@test.com",
			Password = "SuperAdmin123!"
		};

		var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
		var loginResult = await loginResponse.ReadAsJsonAsync<LoginResponse>();

		var refreshRequest = new { RefreshToken = loginResult!.RefreshToken };

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

		// Assert
		response.ShouldBeOk();
		var result = await response.ReadAsJsonAsync<LoginResponse>();
		result.Should().NotBeNull();
		result!.Token.Should().NotBeNullOrEmpty();
		result.RefreshToken.Should().NotBeNullOrEmpty();
		result.RefreshToken.Should().NotBe(loginResult.RefreshToken); // New refresh token
	}

	[Fact]
	public async Task Refresh_WithInvalidRefreshToken_ReturnsUnauthorized()
	{
		// Arrange
		await ResetDatabaseAsync();
		await SeedTestDataAsync();

		var refreshRequest = new { RefreshToken = "invalid-refresh-token" };

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/auth/refresh", refreshRequest);

		// Assert
		response.ShouldBeUnauthorized();
	}

	#endregion

	#region Revoke Token Tests

	[Fact]
	public async Task Revoke_WithValidToken_RevokesRefreshToken()
	{
		// Arrange
		await ResetDatabaseAsync();
		var testData = await SeedTestDataAsync();

		// First login to get a refresh token
		var loginRequest = new
		{
			Email = "superadmin@test.com",
			Password = "SuperAdmin123!"
		};

		var loginResponse = await Client.PostAsJsonAsync("/api/v1/auth/login", loginRequest);
		var loginResult = await loginResponse.ReadAsJsonAsync<LoginResponse>();

		AuthenticateAs(testData.SuperAdmin);
		var revokeRequest = new { RefreshToken = loginResult!.RefreshToken };

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/auth/revoke", revokeRequest);

		// Assert
		response.ShouldBeOk();

		// Verify token is revoked by trying to refresh
		ClearAuthentication();
		var refreshResponse = await Client.PostAsJsonAsync("/api/v1/auth/refresh", revokeRequest);
		refreshResponse.ShouldBeUnauthorized();
	}

	[Fact]
	public async Task Revoke_WithoutAuthentication_ReturnsUnauthorized()
	{
		// Arrange
		ClearAuthentication();
		var revokeRequest = new { RefreshToken = "some-token" };

		// Act
		var response = await Client.PostAsJsonAsync("/api/v1/auth/revoke", revokeRequest);

		// Assert
		response.ShouldBeUnauthorized();
	}

	#endregion

	#region Response DTOs

	private class LoginResponse
	{
		public string Token { get; set; } = string.Empty;
		public string RefreshToken { get; set; } = string.Empty;
		public int UserId { get; set; }
		public string Email { get; set; } = string.Empty;
		public List<string> Permissions { get; set; } = new();
		public bool IsSuperAdmin { get; set; }
	}

	private class MeResponse
	{
		public int Id { get; set; }
		public string Email { get; set; } = string.Empty;
		public bool IsActive { get; set; }
		public bool IsSuperAdmin { get; set; }
		public List<string> Roles { get; set; } = new();
		public List<string> Permissions { get; set; } = new();
	}

	#endregion
}
