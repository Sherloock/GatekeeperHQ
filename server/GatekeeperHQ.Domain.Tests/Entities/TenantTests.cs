using FluentAssertions;
using GatekeeperHQ.Domain.Entities;
using GatekeeperHQ.Tests.Common.Builders;
using Xunit;

namespace GatekeeperHQ.Domain.Tests.Entities;

[Trait("Category", "Unit")]
public class TenantTests
{
	[Fact]
	public void NewTenant_HasCorrectDefaults()
	{
		// Arrange & Act
		var tenant = new Tenant
		{
			Name = "Test Tenant"
		};

		// Assert
		tenant.Id.Should().Be(0);
		tenant.ApiKey.Should().BeNull();
		tenant.IsActive.Should().BeTrue();
		tenant.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
		tenant.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
		tenant.Users.Should().BeEmpty();
		tenant.Roles.Should().BeEmpty();
		tenant.Permissions.Should().BeEmpty();
	}

	[Fact]
	public void InactiveTenant_HasIsActiveFalse()
	{
		// Arrange & Act
		var tenant = TenantBuilder.Default()
			.AsInactive()
			.Build();

		// Assert
		tenant.IsActive.Should().BeFalse();
	}

	[Fact]
	public void TenantBuilder_GeneratesUniqueApiKeys()
	{
		// Arrange & Act
		var tenant1 = TenantBuilder.Default().Build();
		var tenant2 = TenantBuilder.Default().Build();

		// Assert
		tenant1.ApiKey.Should().NotBeNullOrEmpty();
		tenant2.ApiKey.Should().NotBeNullOrEmpty();
		tenant1.ApiKey.Should().NotBe(tenant2.ApiKey);
	}

	[Fact]
	public void TenantBuilder_WithApiKey_SetsSpecificKey()
	{
		// Arrange
		const string specificKey = "my-custom-api-key";

		// Act
		var tenant = TenantBuilder.Default()
			.WithApiKey(specificKey)
			.Build();

		// Assert
		tenant.ApiKey.Should().Be(specificKey);
	}

	[Fact]
	public void TenantBuilder_WithName_SetsSpecificName()
	{
		// Arrange
		const string name = "Custom Tenant Name";

		// Act
		var tenant = TenantBuilder.Default()
			.WithName(name)
			.Build();

		// Assert
		tenant.Name.Should().Be(name);
	}
}
