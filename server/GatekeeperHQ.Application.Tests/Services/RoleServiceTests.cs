using FluentAssertions;
using GatekeeperHQ.Application.Services;
using GatekeeperHQ.Domain.Entities;
using GatekeeperHQ.Tests.Common.Builders;
using GatekeeperHQ.Tests.Common.Fixtures;
using Xunit;

namespace GatekeeperHQ.Application.Tests.Services;

[Trait("Category", "Unit")]
public class RoleServiceTests : IDisposable
{
	private readonly Infrastructure.Data.AppDbContext _context;
	private readonly MockTenantContext _tenantContext;
	private readonly RoleService _roleService;

	public RoleServiceTests()
	{
		_context = InMemoryDbContextFactory.Create();
		_tenantContext = new MockTenantContext();
		_roleService = new RoleService(_context, _tenantContext);
	}

	public void Dispose()
	{
		_context.Dispose();
	}

	[Fact]
	public async Task GetAllRolesAsync_WithoutTenantContext_ThrowsInvalidOperationException()
	{
		// Act & Assert
		await Assert.ThrowsAsync<InvalidOperationException>(() => _roleService.GetAllRolesAsync());
	}

	[Fact]
	public async Task GetAllRolesAsync_ReturnsTenantRoles()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var role1 = RoleBuilder.Default().WithTenantId(tenant.Id).WithName("Admin").Build();
		var role2 = RoleBuilder.Default().WithTenantId(tenant.Id).WithName("Editor").Build();
		_context.Roles.AddRange(role1, role2);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		// Act
		var result = await _roleService.GetAllRolesAsync();

		// Assert
		result.Should().HaveCount(2);
		result.Select(r => r.Name).Should().Contain(new[] { "Admin", "Editor" });
	}

	[Fact]
	public async Task CreateRoleAsync_CreatesRole()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		_tenantContext.SetTenantId(tenant.Id);

		var request = new CreateRoleRequest
		{
			Name = "NewRole",
			Description = "A new role",
			PermissionIds = new List<int>()
		};

		// Act
		var result = await _roleService.CreateRoleAsync(request);

		// Assert
		result.Should().NotBeNull();
		result.Name.Should().Be("NewRole");
		result.Description.Should().Be("A new role");
	}

	[Fact]
	public async Task CreateRoleAsync_WithPermissions_AssignsPermissions()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var permission = PermissionBuilder.UsersView().WithTenantId(tenant.Id).Build();
		_context.Permissions.Add(permission);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		var request = new CreateRoleRequest
		{
			Name = "NewRole",
			PermissionIds = new List<int> { permission.Id }
		};

		// Act
		var result = await _roleService.CreateRoleAsync(request);

		// Assert
		result.Permissions.Should().HaveCount(1);
		result.Permissions.Should().Contain("users.view");
	}

	[Fact]
	public async Task CreateRoleAsync_ThrowsException_WhenNameExists()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var existingRole = RoleBuilder.Default().WithTenantId(tenant.Id).WithName("Admin").Build();
		_context.Roles.Add(existingRole);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		var request = new CreateRoleRequest { Name = "Admin" };

		// Act & Assert
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => _roleService.CreateRoleAsync(request));
		exception.Message.Should().Be("Role name already exists");
	}

	[Fact]
	public async Task UpdateRoleAsync_UpdatesName()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var role = RoleBuilder.Default().WithTenantId(tenant.Id).WithName("OldName").Build();
		_context.Roles.Add(role);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		var request = new UpdateRoleRequest { Name = "NewName" };

		// Act
		var result = await _roleService.UpdateRoleAsync(role.Id, request);

		// Assert
		result.Should().NotBeNull();
		result!.Name.Should().Be("NewName");
	}

	[Fact]
	public async Task DeleteRoleAsync_RemovesRole()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var role = RoleBuilder.Default().WithTenantId(tenant.Id).WithName("ToDelete").Build();
		_context.Roles.Add(role);
		await _context.SaveChangesAsync();

		var roleId = role.Id;
		_tenantContext.SetTenantId(tenant.Id);

		// Act
		var result = await _roleService.DeleteRoleAsync(roleId);

		// Assert
		result.Should().BeTrue();
		var dbRole = await _context.Roles.FindAsync(roleId);
		dbRole.Should().BeNull();
	}

	[Fact]
	public async Task AddPermissionToRoleAsync_AddsPermission()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var role = RoleBuilder.Default().WithTenantId(tenant.Id).WithName("Admin").Build();
		var permission = PermissionBuilder.UsersView().WithTenantId(tenant.Id).Build();
		_context.Roles.Add(role);
		_context.Permissions.Add(permission);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		// Act
		var result = await _roleService.AddPermissionToRoleAsync(role.Id, permission.Id);

		// Assert
		result.Should().BeTrue();
		var permissions = await _roleService.GetRolePermissionsAsync(role.Id);
		permissions.Should().HaveCount(1);
		permissions[0].Key.Should().Be("users.view");
	}

	[Fact]
	public async Task AddPermissionToRoleAsync_ReturnsFalse_WhenAlreadyAssigned()
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

		// Act
		var result = await _roleService.AddPermissionToRoleAsync(role.Id, permission.Id);

		// Assert
		result.Should().BeFalse();
	}

	[Fact]
	public async Task RemovePermissionFromRoleAsync_RemovesPermission()
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

		// Act
		var result = await _roleService.RemovePermissionFromRoleAsync(role.Id, permission.Id);

		// Assert
		result.Should().BeTrue();
		var permissions = await _roleService.GetRolePermissionsAsync(role.Id);
		permissions.Should().BeEmpty();
	}

	[Fact]
	public async Task GetRolePermissionsAsync_ReturnsPermissions()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var permission1 = PermissionBuilder.UsersView().WithTenantId(tenant.Id).Build();
		var permission2 = PermissionBuilder.UsersEdit().WithTenantId(tenant.Id).Build();
		_context.Permissions.AddRange(permission1, permission2);
		await _context.SaveChangesAsync();

		var role = RoleBuilder.Default().WithTenantId(tenant.Id).WithName("Admin").Build();
		_context.Roles.Add(role);
		await _context.SaveChangesAsync();

		_context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission1.Id });
		_context.RolePermissions.Add(new RolePermission { RoleId = role.Id, PermissionId = permission2.Id });
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		// Act
		var result = await _roleService.GetRolePermissionsAsync(role.Id);

		// Assert
		result.Should().HaveCount(2);
		result.Select(p => p.Key).Should().Contain(new[] { "users.view", "users.edit" });
	}
}
