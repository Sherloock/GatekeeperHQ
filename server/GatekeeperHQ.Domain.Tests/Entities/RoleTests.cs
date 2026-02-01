using FluentAssertions;
using GatekeeperHQ.Domain.Entities;
using GatekeeperHQ.Tests.Common.Builders;
using Xunit;

namespace GatekeeperHQ.Domain.Tests.Entities;

[Trait("Category", "Unit")]
public class RoleTests
{
	[Fact]
	public void NewRole_HasCorrectDefaults()
	{
		// Arrange & Act
		var role = new Role
		{
			TenantId = 1,
			Name = "Test Role"
		};

		// Assert
		role.Id.Should().Be(0);
		role.Description.Should().BeNull();
		role.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
		role.RolePermissions.Should().BeEmpty();
		role.UserRoles.Should().BeEmpty();
	}

	[Fact]
	public void Role_CanHaveMultiplePermissions()
	{
		// Arrange
		var tenant = TenantBuilder.Default().WithId(1).Build();
		var permission1 = PermissionBuilder.UsersView().WithId(1).WithTenant(tenant).Build();
		var permission2 = PermissionBuilder.UsersEdit().WithId(2).WithTenant(tenant).Build();

		// Act
		var role = RoleBuilder.Default()
			.WithTenant(tenant)
			.WithPermission(permission1)
			.WithPermission(permission2)
			.Build();

		// Assert
		role.RolePermissions.Should().HaveCount(2);
		role.RolePermissions.Select(rp => rp.Permission.Key)
			.Should().Contain(new[] { "users.view", "users.edit" });
	}

	[Fact]
	public void RoleBuilder_WithDescription_SetsDescription()
	{
		// Arrange
		const string description = "A test role description";

		// Act
		var role = RoleBuilder.Default()
			.WithTenantId(1)
			.WithDescription(description)
			.Build();

		// Assert
		role.Description.Should().Be(description);
	}

	[Fact]
	public void RoleBuilder_WithName_SetsName()
	{
		// Arrange
		const string name = "CustomRole";

		// Act
		var role = RoleBuilder.Default()
			.WithTenantId(1)
			.WithName(name)
			.Build();

		// Assert
		role.Name.Should().Be(name);
	}
}
