using FluentAssertions;
using GatekeeperHQ.Application.Services;
using GatekeeperHQ.Tests.Common.Builders;
using GatekeeperHQ.Tests.Common.Fixtures;
using Xunit;

namespace GatekeeperHQ.Application.Tests.Services;

[Trait("Category", "Unit")]
public class ApiKeyServiceTests : IDisposable
{
	private readonly Infrastructure.Data.AppDbContext _context;
	private readonly MockTenantContext _tenantContext;
	private readonly ApiKeyService _apiKeyService;

	public ApiKeyServiceTests()
	{
		_context = InMemoryDbContextFactory.Create();
		_tenantContext = new MockTenantContext();
		_apiKeyService = new ApiKeyService(_context, _tenantContext);
	}

	public void Dispose()
	{
		_context.Dispose();
	}

	[Fact]
	public async Task GetAllApiKeysAsync_WithoutTenantContext_ThrowsInvalidOperationException()
	{
		// Act & Assert
		await Assert.ThrowsAsync<InvalidOperationException>(() => _apiKeyService.GetAllApiKeysAsync());
	}

	[Fact]
	public async Task GetAllApiKeysAsync_ReturnsTenantApiKeys()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var apiKey1 = ApiKeyBuilder.Default().WithTenantId(tenant.Id).WithName("Key 1").Build();
		var apiKey2 = ApiKeyBuilder.Default().WithTenantId(tenant.Id).WithName("Key 2").Build();
		_context.ApiKeys.AddRange(apiKey1, apiKey2);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		// Act
		var result = await _apiKeyService.GetAllApiKeysAsync();

		// Assert
		result.Should().HaveCount(2);
	}

	[Fact]
	public async Task CreateApiKeyAsync_ReturnsPlainTextKeyOnce()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		_tenantContext.SetTenantId(tenant.Id);

		var request = new CreateApiKeyRequest
		{
			Name = "Test Key",
			Permissions = new List<string> { "users.view" },
			IsActive = true
		};

		// Act
		var result = await _apiKeyService.CreateApiKeyAsync(request);

		// Assert
		result.Should().NotBeNull();
		result.Key.Should().StartWith("gk_");
		result.Key.Should().NotBeNullOrEmpty();
		result.Name.Should().Be("Test Key");
	}

	[Fact]
	public async Task CreateApiKeyAsync_StoresHashedKey()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		_tenantContext.SetTenantId(tenant.Id);

		var request = new CreateApiKeyRequest { Name = "Test Key" };

		// Act
		var result = await _apiKeyService.CreateApiKeyAsync(request);

		// Assert
		var dbApiKey = await _context.ApiKeys.FindAsync(result.Id);
		dbApiKey.Should().NotBeNull();
		dbApiKey!.KeyHash.Should().NotBe(result.Key);
		dbApiKey.KeyHash.Should().HaveLength(64); // SHA256 hex
	}

	[Fact]
	public async Task CreateApiKeyAsync_StoresPermissions()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		_tenantContext.SetTenantId(tenant.Id);

		var request = new CreateApiKeyRequest
		{
			Name = "Test Key",
			Permissions = new List<string> { "users.view", "roles.view" }
		};

		// Act
		var result = await _apiKeyService.CreateApiKeyAsync(request);

		// Assert
		result.Permissions.Should().HaveCount(2);
		result.Permissions.Should().Contain(new[] { "users.view", "roles.view" });
	}

	[Fact]
	public async Task ValidateApiKeyAsync_ReturnsApiKey_WhenValid()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		_tenantContext.SetTenantId(tenant.Id);

		var createRequest = new CreateApiKeyRequest { Name = "Valid Key" };
		var createResult = await _apiKeyService.CreateApiKeyAsync(createRequest);

		// Act
		var result = await _apiKeyService.ValidateApiKeyAsync(createResult.Key);

		// Assert
		result.Should().NotBeNull();
		result!.Name.Should().Be("Valid Key");
	}

	[Fact]
	public async Task ValidateApiKeyAsync_ReturnsNull_WhenInactive()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		_tenantContext.SetTenantId(tenant.Id);

		var createRequest = new CreateApiKeyRequest { Name = "Inactive Key", IsActive = false };
		var createResult = await _apiKeyService.CreateApiKeyAsync(createRequest);

		// Act
		var result = await _apiKeyService.ValidateApiKeyAsync(createResult.Key);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task ValidateApiKeyAsync_ReturnsNull_WhenExpired()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		_tenantContext.SetTenantId(tenant.Id);

		var createRequest = new CreateApiKeyRequest
		{
			Name = "Expired Key",
			ExpiresAt = DateTime.UtcNow.AddDays(-1)
		};
		var createResult = await _apiKeyService.CreateApiKeyAsync(createRequest);

		// Act
		var result = await _apiKeyService.ValidateApiKeyAsync(createResult.Key);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task ValidateApiKeyAsync_ReturnsNull_WhenKeyNotFound()
	{
		// Act
		var result = await _apiKeyService.ValidateApiKeyAsync("nonexistent-key");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task RecordApiKeyUsageAsync_UpdatesLastUsedAt()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		_tenantContext.SetTenantId(tenant.Id);

		var createRequest = new CreateApiKeyRequest { Name = "Track Usage" };
		var createResult = await _apiKeyService.CreateApiKeyAsync(createRequest);

		var dbApiKey = await _context.ApiKeys.FindAsync(createResult.Id);
		dbApiKey!.LastUsedAt.Should().BeNull();

		// Act
		await _apiKeyService.RecordApiKeyUsageAsync(createResult.Id);

		// Assert
		await _context.Entry(dbApiKey).ReloadAsync();
		dbApiKey.LastUsedAt.Should().NotBeNull();
		dbApiKey.LastUsedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
	}

	[Fact]
	public async Task UpdateApiKeyAsync_UpdatesName()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		_tenantContext.SetTenantId(tenant.Id);

		var createRequest = new CreateApiKeyRequest { Name = "Old Name" };
		var createResult = await _apiKeyService.CreateApiKeyAsync(createRequest);

		var updateRequest = new UpdateApiKeyRequest { Name = "New Name" };

		// Act
		var result = await _apiKeyService.UpdateApiKeyAsync(createResult.Id, updateRequest);

		// Assert
		result.Should().NotBeNull();
		result!.Name.Should().Be("New Name");
	}

	[Fact]
	public async Task UpdateApiKeyAsync_UpdatesIsActive()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		_tenantContext.SetTenantId(tenant.Id);

		var createRequest = new CreateApiKeyRequest { Name = "Key", IsActive = true };
		var createResult = await _apiKeyService.CreateApiKeyAsync(createRequest);

		var updateRequest = new UpdateApiKeyRequest { IsActive = false };

		// Act
		var result = await _apiKeyService.UpdateApiKeyAsync(createResult.Id, updateRequest);

		// Assert
		result.Should().NotBeNull();
		result!.IsActive.Should().BeFalse();
	}

	[Fact]
	public async Task DeleteApiKeyAsync_DeletesKey()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		_tenantContext.SetTenantId(tenant.Id);

		var createRequest = new CreateApiKeyRequest { Name = "To Delete" };
		var createResult = await _apiKeyService.CreateApiKeyAsync(createRequest);

		// Act
		var result = await _apiKeyService.DeleteApiKeyAsync(createResult.Id);

		// Assert
		result.Should().BeTrue();
		var dbApiKey = await _context.ApiKeys.FindAsync(createResult.Id);
		dbApiKey.Should().BeNull();
	}

	[Fact]
	public async Task DeleteApiKeyAsync_ReturnsFalse_WhenNotFound()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		_tenantContext.SetTenantId(tenant.Id);

		// Act
		var result = await _apiKeyService.DeleteApiKeyAsync(999);

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task GetApiKeyByIdAsync_ReturnsApiKey()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		_tenantContext.SetTenantId(tenant.Id);

		var createRequest = new CreateApiKeyRequest { Name = "Get By Id" };
		var createResult = await _apiKeyService.CreateApiKeyAsync(createRequest);

		// Act
		var result = await _apiKeyService.GetApiKeyByIdAsync(createResult.Id);

		// Assert
		result.Should().NotBeNull();
		result!.Name.Should().Be("Get By Id");
	}
}
