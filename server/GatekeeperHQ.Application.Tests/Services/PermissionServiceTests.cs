using FluentAssertions;
using GatekeeperHQ.Application.Services;
using GatekeeperHQ.Domain.Entities;
using GatekeeperHQ.Tests.Common.Builders;
using GatekeeperHQ.Tests.Common.Fixtures;
using Xunit;

namespace GatekeeperHQ.Application.Tests.Services;

[Trait("Category", "Unit")]
public class PermissionServiceTests : IDisposable
{
	private readonly Infrastructure.Data.AppDbContext _context;
	private readonly MockTenantContext _tenantContext;
	private readonly PermissionService _permissionService;

	public PermissionServiceTests()
	{
		_context = InMemoryDbContextFactory.Create();
		_tenantContext = new MockTenantContext();
		_permissionService = new PermissionService(_context, _tenantContext);
	}

	public void Dispose()
	{
		_context.Dispose();
	}

	[Fact]
	public async Task GetAllAsync_WithoutTenantContext_ThrowsInvalidOperationException()
	{
		// Act & Assert
		await Assert.ThrowsAsync<InvalidOperationException>(() => _permissionService.GetAllAsync());
	}

	[Fact]
	public async Task GetAllAsync_ReturnsTenantPermissions()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var permission1 = PermissionBuilder.UsersView().WithTenantId(tenant.Id).Build();
		var permission2 = PermissionBuilder.RolesView().WithTenantId(tenant.Id).Build();
		_context.Permissions.AddRange(permission1, permission2);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		// Act
		var result = await _permissionService.GetAllAsync();

		// Assert
		result.Should().HaveCount(2);
	}

	[Fact]
	public async Task CreateAsync_CreatesPermission()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		_tenantContext.SetTenantId(tenant.Id);

		var request = new CreatePermissionRequest
		{
			Key = "reports.view",
			Description = "View reports"
		};

		// Act
		var result = await _permissionService.CreateAsync(request);

		// Assert
		result.Should().NotBeNull();
		result.Key.Should().Be("reports.view");
		result.Description.Should().Be("View reports");
	}

	[Fact]
	public async Task CreateAsync_LowercasesKey()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		_tenantContext.SetTenantId(tenant.Id);

		var request = new CreatePermissionRequest { Key = "REPORTS.VIEW" };

		// Act
		var result = await _permissionService.CreateAsync(request);

		// Assert
		result.Key.Should().Be("reports.view");
	}

	[Fact]
	public async Task CreateAsync_ThrowsException_WhenKeyFormatInvalid()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		_tenantContext.SetTenantId(tenant.Id);

		var request = new CreatePermissionRequest { Key = "invalidformat" };

		// Act & Assert
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => _permissionService.CreateAsync(request));
		exception.Message.Should().Contain("resource.action");
	}

	[Fact]
	public async Task CreateAsync_ThrowsException_WhenKeyExists()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var existing = PermissionBuilder.UsersView().WithTenantId(tenant.Id).Build();
		_context.Permissions.Add(existing);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		var request = new CreatePermissionRequest { Key = "users.view" };

		// Act & Assert
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => _permissionService.CreateAsync(request));
		exception.Message.Should().Be("Permission key already exists");
	}

	[Fact]
	public async Task UpdateAsync_UpdatesKey()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var permission = PermissionBuilder.Default().WithTenantId(tenant.Id).WithKey("old.key").Build();
		_context.Permissions.Add(permission);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		var request = new UpdatePermissionRequest { Key = "new.key" };

		// Act
		var result = await _permissionService.UpdateAsync(permission.Id, request);

		// Assert
		result.Should().NotBeNull();
		result!.Key.Should().Be("new.key");
	}

	[Fact]
	public async Task DeleteAsync_DeletesPermission()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var permission = PermissionBuilder.Default().WithTenantId(tenant.Id).WithKey("test.delete").Build();
		_context.Permissions.Add(permission);
		await _context.SaveChangesAsync();

		var permissionId = permission.Id;
		_tenantContext.SetTenantId(tenant.Id);

		// Act
		var result = await _permissionService.DeleteAsync(permissionId);

		// Assert
		result.Should().BeTrue();
		var dbPermission = await _context.Permissions.FindAsync(permissionId);
		dbPermission.Should().BeNull();
	}

	[Fact]
	public async Task DeleteAsync_ThrowsException_WhenPermissionInUse()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var permission = PermissionBuilder.UsersView().WithTenantId(tenant.Id).Build();
		_context.Permissions.Add(permission);
		await _context.SaveChangesAsync();

		var role = RoleBuilder.Default().WithTenantId(tenant.Id).WithName("Admin").Build();
		_context.Roles.Add(role);
		await _context.SaveChangesAsync();

		_context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission.Id });
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		// Act & Assert
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => _permissionService.DeleteAsync(permission.Id));
		exception.Message.Should().Contain("Cannot delete permission");
	}

	[Fact]
	public async Task GetByIdAsync_ReturnsPermission()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var permission = PermissionBuilder.UsersView().WithTenantId(tenant.Id).Build();
		_context.Permissions.Add(permission);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		// Act
		var result = await _permissionService.GetByIdAsync(permission.Id);

		// Assert
		result.Should().NotBeNull();
		result!.Key.Should().Be("users.view");
	}

	[Fact]
	public async Task GetByIdAsync_ReturnsNull_WhenNotFound()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		_tenantContext.SetTenantId(tenant.Id);

		// Act
		var result = await _permissionService.GetByIdAsync(999);

		// Assert
		result.Should().BeNull();
	}
}
