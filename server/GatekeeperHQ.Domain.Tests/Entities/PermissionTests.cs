using FluentAssertions;
using GatekeeperHQ.Domain.Entities;
using GatekeeperHQ.Tests.Common.Builders;
using Xunit;

namespace GatekeeperHQ.Domain.Tests.Entities;

[Trait("Category", "Unit")]
public class PermissionTests
{
	[Fact]
	public void NewPermission_HasCorrectDefaults()
	{
		// Arrange & Act
		var permission = new Permission
		{
			TenantId = 1,
			Key = "users.view"
		};

		// Assert
		permission.Id.Should().Be(0);
		permission.Description.Should().BeNull();
		permission.RolePermissions.Should().BeEmpty();
	}

	[Theory]
	[InlineData("users.view")]
	[InlineData("users.create")]
	[InlineData("users.edit")]
	[InlineData("users.delete")]
	[InlineData("roles.view")]
	[InlineData("roles.manage")]
	[InlineData("permissions.view")]
	[InlineData("permissions.create")]
	[InlineData("permissions.manage")]
	[InlineData("dashboard.access")]
	[InlineData("settings.access")]
	[InlineData("tenants.view")]
	[InlineData("tenants.create")]
	[InlineData("tenants.manage")]
	[InlineData("invitations.manage")]
	public void PermissionKey_ValidFormats(string key)
	{
		// Arrange & Act
		var permission = PermissionBuilder.Default()
			.WithTenantId(1)
			.WithKey(key)
			.Build();

		// Assert
		permission.Key.Should().Be(key);
		permission.Key.Should().Contain(".");
	}

	[Fact]
	public void PermissionBuilder_WithDescription_SetsDescription()
	{
		// Arrange
		const string description = "Allows viewing users";

		// Act
		var permission = PermissionBuilder.Default()
			.WithTenantId(1)
			.WithDescription(description)
			.Build();

		// Assert
		permission.Description.Should().Be(description);
	}

	[Fact]
	public void PermissionBuilder_UsersView_CreatesCorrectKey()
	{
		// Arrange & Act
		var permission = PermissionBuilder.UsersView()
			.WithTenantId(1)
			.Build();

		// Assert
		permission.Key.Should().Be("users.view");
	}

	[Fact]
	public void PermissionBuilder_RolesManage_CreatesCorrectKey()
	{
		// Arrange & Act
		var permission = PermissionBuilder.RolesManage()
			.WithTenantId(1)
			.Build();

		// Assert
		permission.Key.Should().Be("roles.manage");
	}
}
