using FluentAssertions;
using GatekeeperHQ.Application.Services;
using GatekeeperHQ.Tests.Common.Builders;
using GatekeeperHQ.Tests.Common.Fixtures;
using Xunit;

namespace GatekeeperHQ.Application.Tests.Services;

[Trait("Category", "Unit")]
public class TenantServiceTests : IDisposable
{
	private readonly Infrastructure.Data.AppDbContext _context;
	private readonly TenantService _tenantService;

	public TenantServiceTests()
	{
		_context = InMemoryDbContextFactory.Create();
		_tenantService = new TenantService(_context);
	}

	public void Dispose()
	{
		_context.Dispose();
	}

	[Fact]
	public async Task GetAllTenantsAsync_ReturnsAllTenants()
	{
		// Arrange
		var tenant1 = TenantBuilder.Default().WithName("Tenant A").Build();
		var tenant2 = TenantBuilder.Default().WithName("Tenant B").Build();
		_context.Tenants.AddRange(tenant1, tenant2);
		await _context.SaveChangesAsync();

		// Act
		var result = await _tenantService.GetAllTenantsAsync();

		// Assert
		result.Should().HaveCount(2);
		result.Select(t => t.Name).Should().Contain(new[] { "Tenant A", "Tenant B" });
	}

	[Fact]
	public async Task GetTenantByIdAsync_ReturnsTenant()
	{
		// Arrange
		var tenant = TenantBuilder.Default().WithName("Test Tenant").Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		// Act
		var result = await _tenantService.GetTenantByIdAsync(tenant.Id);

		// Assert
		result.Should().NotBeNull();
		result!.Name.Should().Be("Test Tenant");
	}

	[Fact]
	public async Task GetTenantByIdAsync_ReturnsNull_WhenNotFound()
	{
		// Act
		var result = await _tenantService.GetTenantByIdAsync(999);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task CreateTenantAsync_CreatesTenantWithApiKey()
	{
		// Arrange
		var request = new CreateTenantRequest
		{
			Name = "New Tenant",
			IsActive = true
		};

		// Act
		var result = await _tenantService.CreateTenantAsync(request);

		// Assert
		result.Should().NotBeNull();
		result.Name.Should().Be("New Tenant");
		result.ApiKey.Should().NotBeNullOrEmpty();
		result.ApiKey.Should().HaveLength(32);  // GUID without dashes
		result.IsActive.Should().BeTrue();
	}

	[Fact]
	public async Task CreateTenantAsync_SeedsDefaultPermissions()
	{
		// Arrange
		var request = new CreateTenantRequest { Name = "New Tenant" };

		// Act
		var result = await _tenantService.CreateTenantAsync(request);

		// Assert
		var permissions = _context.Permissions.Where(p => p.TenantId == result.Id).ToList();
		permissions.Should().NotBeEmpty();
		permissions.Select(p => p.Key).Should().Contain("users.view");
		permissions.Select(p => p.Key).Should().Contain("roles.view");
	}

	[Fact]
	public async Task CreateTenantAsync_ThrowsException_WhenNameExists()
	{
		// Arrange
		var existing = TenantBuilder.Default().WithName("Existing").Build();
		_context.Tenants.Add(existing);
		await _context.SaveChangesAsync();

		var request = new CreateTenantRequest { Name = "Existing" };

		// Act & Assert
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => _tenantService.CreateTenantAsync(request));
		exception.Message.Should().Be("Tenant name already exists");
	}

	[Fact]
	public async Task UpdateTenantAsync_UpdatesName()
	{
		// Arrange
		var tenant = TenantBuilder.Default().WithName("Old Name").Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var request = new UpdateTenantRequest { Name = "New Name" };

		// Act
		var result = await _tenantService.UpdateTenantAsync(tenant.Id, request);

		// Assert
		result.Should().NotBeNull();
		result!.Name.Should().Be("New Name");
	}

	[Fact]
	public async Task UpdateTenantAsync_UpdatesIsActive()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();  // IsActive = true by default
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var request = new UpdateTenantRequest { IsActive = false };

		// Act
		var result = await _tenantService.UpdateTenantAsync(tenant.Id, request);

		// Assert
		result.Should().NotBeNull();
		result!.IsActive.Should().BeFalse();
	}

	[Fact]
	public async Task DeleteTenantAsync_DeletesTenant()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		var tenantId = tenant.Id;

		// Act
		var result = await _tenantService.DeleteTenantAsync(tenantId);

		// Assert
		result.Should().BeTrue();
		var dbTenant = await _context.Tenants.FindAsync(tenantId);
		dbTenant.Should().BeNull();
	}

	[Fact]
	public async Task DeleteTenantAsync_ReturnsFalse_WhenNotFound()
	{
		// Act
		var result = await _tenantService.DeleteTenantAsync(999);

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task RegenerateApiKeyAsync_GeneratesNewKey()
	{
		// Arrange
		var tenant = TenantBuilder.Default().WithApiKey("old-api-key").Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		// Act
		var newKey = await _tenantService.RegenerateApiKeyAsync(tenant.Id);

		// Assert
		newKey.Should().NotBe("old-api-key");
		newKey.Should().HaveLength(32);  // GUID without dashes

		await _context.Entry(tenant).ReloadAsync();
		tenant.ApiKey.Should().Be(newKey);
	}

	[Fact]
	public async Task RegenerateApiKeyAsync_ThrowsException_WhenNotFound()
	{
		// Act & Assert
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => _tenantService.RegenerateApiKeyAsync(999));
		exception.Message.Should().Be("Tenant not found");
	}

	[Fact]
	public async Task RegenerateApiKeyAsync_UpdatesTimestamp()
	{
		// Arrange
		var tenant = TenantBuilder.Default()
			.WithCreatedAt(DateTime.UtcNow.AddDays(-1))
			.Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var originalUpdatedAt = tenant.UpdatedAt;

		// Act
		await _tenantService.RegenerateApiKeyAsync(tenant.Id);

		// Assert
		await _context.Entry(tenant).ReloadAsync();
		tenant.UpdatedAt.Should().BeAfter(originalUpdatedAt);
	}
}
