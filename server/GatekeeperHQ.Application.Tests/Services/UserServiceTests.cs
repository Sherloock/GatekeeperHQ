using FluentAssertions;
using GatekeeperHQ.Application.Services;
using GatekeeperHQ.Tests.Common.Builders;
using GatekeeperHQ.Tests.Common.Fixtures;
using Xunit;

namespace GatekeeperHQ.Application.Tests.Services;

[Trait("Category", "Unit")]
public class UserServiceTests : IDisposable
{
	private readonly Infrastructure.Data.AppDbContext _context;
	private readonly MockTenantContext _tenantContext;
	private readonly UserService _userService;

	public UserServiceTests()
	{
		_context = InMemoryDbContextFactory.Create();
		_tenantContext = new MockTenantContext();
		_userService = new UserService(_context, _tenantContext);
	}

	public void Dispose()
	{
		_context.Dispose();
	}

	[Fact]
	public async Task GetAllUsersAsync_WithoutTenantContext_ThrowsInvalidOperationException()
	{
		// Arrange - no tenant context set

		// Act & Assert
		await Assert.ThrowsAsync<InvalidOperationException>(() => _userService.GetAllUsersAsync());
	}

	[Fact]
	public async Task GetAllUsersAsync_ReturnsTenantUsers()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var user1 = UserBuilder.Default().WithTenantId(tenant.Id).WithEmail("user1@test.com").Build();
		var user2 = UserBuilder.Default().WithTenantId(tenant.Id).WithEmail("user2@test.com").Build();
		_context.Users.AddRange(user1, user2);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		// Act
		var result = await _userService.GetAllUsersAsync();

		// Assert
		result.Should().HaveCount(2);
		result.Select(u => u.Email).Should().Contain(new[] { "user1@test.com", "user2@test.com" });
	}

	[Fact]
	public async Task GetAllUsersAsync_DoesNotReturnOtherTenantUsers()
	{
		// Arrange
		var tenant1 = TenantBuilder.Default().WithName("Tenant 1").Build();
		var tenant2 = TenantBuilder.Default().WithName("Tenant 2").Build();
		_context.Tenants.AddRange(tenant1, tenant2);
		await _context.SaveChangesAsync();

		var user1 = UserBuilder.Default().WithTenantId(tenant1.Id).WithEmail("user1@test.com").Build();
		var user2 = UserBuilder.Default().WithTenantId(tenant2.Id).WithEmail("user2@test.com").Build();
		_context.Users.AddRange(user1, user2);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant1.Id);

		// Act
		var result = await _userService.GetAllUsersAsync();

		// Assert
		result.Should().HaveCount(1);
		result[0].Email.Should().Be("user1@test.com");
	}

	[Fact]
	public async Task GetUserByIdAsync_ReturnsUser()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var user = UserBuilder.Default().WithTenantId(tenant.Id).WithEmail("test@test.com").Build();
		_context.Users.Add(user);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		// Act
		var result = await _userService.GetUserByIdAsync(user.Id);

		// Assert
		result.Should().NotBeNull();
		result!.Email.Should().Be("test@test.com");
	}

	[Fact]
	public async Task GetUserByIdAsync_ReturnsNull_WhenUserNotFound()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		_tenantContext.SetTenantId(tenant.Id);

		// Act
		var result = await _userService.GetUserByIdAsync(999);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task GetUserByIdAsync_ReturnsNull_WhenUserBelongsToDifferentTenant()
	{
		// Arrange
		var tenant1 = TenantBuilder.Default().WithName("Tenant 1").Build();
		var tenant2 = TenantBuilder.Default().WithName("Tenant 2").Build();
		_context.Tenants.AddRange(tenant1, tenant2);
		await _context.SaveChangesAsync();

		var user = UserBuilder.Default().WithTenantId(tenant2.Id).WithEmail("test@test.com").Build();
		_context.Users.Add(user);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant1.Id);

		// Act
		var result = await _userService.GetUserByIdAsync(user.Id);

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task CreateUserAsync_CreatesUserWithHashedPassword()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		_tenantContext.SetTenantId(tenant.Id);

		var request = new CreateUserRequest
		{
			Email = "newuser@test.com",
			Password = "TestPassword123!",
			IsActive = true,
			RoleIds = new List<int>()
		};

		// Act
		var result = await _userService.CreateUserAsync(request);

		// Assert
		result.Should().NotBeNull();
		result.Email.Should().Be("newuser@test.com");

		var dbUser = await _context.Users.FindAsync(result.Id);
		dbUser.Should().NotBeNull();
		dbUser!.PasswordHash.Should().NotBe("TestPassword123!");
		BCrypt.Net.BCrypt.Verify("TestPassword123!", dbUser.PasswordHash).Should().BeTrue();
	}

	[Fact]
	public async Task CreateUserAsync_ThrowsException_WhenEmailExists()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var existingUser = UserBuilder.Default().WithTenantId(tenant.Id).WithEmail("existing@test.com").Build();
		_context.Users.Add(existingUser);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		var request = new CreateUserRequest
		{
			Email = "existing@test.com",
			Password = "Password123!",
			IsActive = true
		};

		// Act & Assert
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => _userService.CreateUserAsync(request));
		exception.Message.Should().Be("Email already exists");
	}

	[Fact]
	public async Task CreateUserAsync_AssignsRoles()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var role = RoleBuilder.Default().WithTenantId(tenant.Id).WithName("Admin").Build();
		_context.Roles.Add(role);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		var request = new CreateUserRequest
		{
			Email = "newuser@test.com",
			Password = "TestPassword123!",
			IsActive = true,
			RoleIds = new List<int> { role.Id }
		};

		// Act
		var result = await _userService.CreateUserAsync(request);

		// Assert
		result.Roles.Should().HaveCount(1);
		result.Roles.Should().Contain("Admin");
	}

	[Fact]
	public async Task UpdateUserAsync_UpdatesEmail()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var user = UserBuilder.Default().WithTenantId(tenant.Id).WithEmail("old@test.com").Build();
		_context.Users.Add(user);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		var request = new UpdateUserRequest { Email = "new@test.com" };

		// Act
		var result = await _userService.UpdateUserAsync(user.Id, request);

		// Assert
		result.Should().NotBeNull();
		result!.Email.Should().Be("new@test.com");
	}

	[Fact]
	public async Task UpdateUserAsync_UpdatesPassword()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var user = UserBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithEmail("test@test.com")
			.WithPassword("OldPassword123!")
			.Build();
		_context.Users.Add(user);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		var request = new UpdateUserRequest { Password = "NewPassword123!" };

		// Act
		await _userService.UpdateUserAsync(user.Id, request);

		// Assert
		await _context.Entry(user).ReloadAsync();
		BCrypt.Net.BCrypt.Verify("NewPassword123!", user.PasswordHash).Should().BeTrue();
	}

	[Fact]
	public async Task DeleteUserAsync_RemovesUser()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var user = UserBuilder.Default().WithTenantId(tenant.Id).WithEmail("test@test.com").Build();
		_context.Users.Add(user);
		await _context.SaveChangesAsync();

		var userId = user.Id;
		_tenantContext.SetTenantId(tenant.Id);

		// Act
		var result = await _userService.DeleteUserAsync(userId);

		// Assert
		result.Should().BeTrue();
		var dbUser = await _context.Users.FindAsync(userId);
		dbUser.Should().BeNull();
	}

	[Fact]
	public async Task DeleteUserAsync_ReturnsFalse_WhenUserNotFound()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		_tenantContext.SetTenantId(tenant.Id);

		// Act
		var result = await _userService.DeleteUserAsync(999);

		// Assert
		result.Should().BeFalse();
	}
}
