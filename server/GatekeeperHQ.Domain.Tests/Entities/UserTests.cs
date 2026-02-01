using FluentAssertions;
using GatekeeperHQ.Domain.Entities;
using GatekeeperHQ.Tests.Common.Builders;
using Xunit;

namespace GatekeeperHQ.Domain.Tests.Entities;

[Trait("Category", "Unit")]
public class UserTests
{
	[Fact]
	public void NewUser_HasCorrectDefaults()
	{
		// Arrange & Act
		var user = new User
		{
			Email = "test@example.com",
			PasswordHash = "hash"
		};

		// Assert
		user.Id.Should().Be(0);
		user.TenantId.Should().BeNull();
		user.IsActive.Should().BeTrue();
		user.IsSuperAdmin.Should().BeFalse();
		user.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
		user.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
		user.UserRoles.Should().BeEmpty();
	}

	[Fact]
	public void SuperAdmin_HasNoTenant()
	{
		// Arrange & Act
		var superAdmin = UserBuilder.SuperAdmin()
			.WithEmail("admin@test.com")
			.Build();

		// Assert
		superAdmin.IsSuperAdmin.Should().BeTrue();
		superAdmin.TenantId.Should().BeNull();
		superAdmin.Tenant.Should().BeNull();
	}

	[Fact]
	public void TenantUser_HasTenant()
	{
		// Arrange
		var tenant = TenantBuilder.Default().WithId(1).Build();

		// Act
		var user = UserBuilder.Default()
			.WithTenant(tenant)
			.Build();

		// Assert
		user.IsSuperAdmin.Should().BeFalse();
		user.TenantId.Should().Be(1);
		user.Tenant.Should().Be(tenant);
	}

	[Fact]
	public void User_CanHaveMultipleRoles()
	{
		// Arrange
		var tenant = TenantBuilder.Default().WithId(1).Build();
		var role1 = RoleBuilder.Default().WithId(1).WithTenant(tenant).WithName("Admin").Build();
		var role2 = RoleBuilder.Default().WithId(2).WithTenant(tenant).WithName("Editor").Build();

		// Act
		var user = UserBuilder.Default()
			.WithTenant(tenant)
			.WithRole(role1)
			.WithRole(role2)
			.Build();

		// Assert
		user.UserRoles.Should().HaveCount(2);
		user.UserRoles.Select(ur => ur.Role.Name).Should().Contain(new[] { "Admin", "Editor" });
	}

	[Fact]
	public void InactiveUser_HasIsActiveFalse()
	{
		// Arrange & Act
		var user = UserBuilder.Default()
			.AsInactive()
			.Build();

		// Assert
		user.IsActive.Should().BeFalse();
	}

	[Fact]
	public void UserBuilder_WithPassword_HashesPassword()
	{
		// Arrange & Act
		var user = UserBuilder.Default()
			.WithPassword("TestPassword123!")
			.Build();

		// Assert
		user.PasswordHash.Should().NotBe("TestPassword123!");
		user.PasswordHash.Should().StartWith("$2");  // BCrypt hash prefix
		BCrypt.Net.BCrypt.Verify("TestPassword123!", user.PasswordHash).Should().BeTrue();
	}
}
