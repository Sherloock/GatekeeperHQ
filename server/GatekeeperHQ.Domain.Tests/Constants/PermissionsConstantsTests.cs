using FluentAssertions;
using GatekeeperHQ.Domain.Constants;
using Xunit;

namespace GatekeeperHQ.Domain.Tests.Constants;

[Trait("Category", "Unit")]
public class PermissionsConstantsTests
{
	[Fact]
	public void TenantPermissions_ContainsExpectedPermissions()
	{
		// Arrange & Act & Assert
		Permissions.TenantPermissions.Should().Contain(Permissions.UsersView);
		Permissions.TenantPermissions.Should().Contain(Permissions.UsersCreate);
		Permissions.TenantPermissions.Should().Contain(Permissions.UsersEdit);
		Permissions.TenantPermissions.Should().Contain(Permissions.UsersDelete);
		Permissions.TenantPermissions.Should().Contain(Permissions.RolesView);
		Permissions.TenantPermissions.Should().Contain(Permissions.RolesManage);
		Permissions.TenantPermissions.Should().Contain(Permissions.PermissionsView);
		Permissions.TenantPermissions.Should().Contain(Permissions.PermissionsCreate);
		Permissions.TenantPermissions.Should().Contain(Permissions.PermissionsManage);
		Permissions.TenantPermissions.Should().Contain(Permissions.DashboardAccess);
		Permissions.TenantPermissions.Should().Contain(Permissions.SettingsAccess);
	}

	[Fact]
	public void SuperAdminPermissions_ContainsExpectedPermissions()
	{
		// Arrange & Act & Assert
		Permissions.SuperAdminPermissions.Should().Contain(Permissions.TenantsView);
		Permissions.SuperAdminPermissions.Should().Contain(Permissions.TenantsCreate);
		Permissions.SuperAdminPermissions.Should().Contain(Permissions.TenantsManage);
		Permissions.SuperAdminPermissions.Should().Contain(Permissions.InvitationsManage);
	}

	[Fact]
	public void AllPermissions_ContainsBothTenantAndSuperAdminPermissions()
	{
		// Arrange & Act & Assert
		Permissions.All.Should().Contain(Permissions.TenantPermissions);
		Permissions.All.Should().Contain(Permissions.SuperAdminPermissions);
	}

	[Fact]
	public void AllPermissions_HasNoDuplicates()
	{
		// Arrange & Act & Assert
		Permissions.All.Should().OnlyHaveUniqueItems();
	}

	[Theory]
	[InlineData("users.view")]
	[InlineData("users.create")]
	[InlineData("users.edit")]
	[InlineData("users.delete")]
	[InlineData("roles.view")]
	[InlineData("roles.manage")]
	[InlineData("tenants.view")]
	[InlineData("tenants.create")]
	[InlineData("tenants.manage")]
	[InlineData("invitations.manage")]
	public void PermissionKey_FollowsResourceActionFormat(string permissionKey)
	{
		// Arrange & Act
		var parts = permissionKey.Split('.');

		// Assert
		parts.Should().HaveCount(2);
		parts[0].Should().NotBeNullOrWhiteSpace();  // Resource
		parts[1].Should().NotBeNullOrWhiteSpace();  // Action
	}
}
