using FluentAssertions;
using GatekeeperHQ.Domain.Entities;
using GatekeeperHQ.Tests.Common.Builders;
using Xunit;

namespace GatekeeperHQ.Domain.Tests.Entities;

[Trait("Category", "Unit")]
public class RefreshTokenTests
{
	[Fact]
	public void NewRefreshToken_HasCorrectDefaults()
	{
		// Arrange & Act
		var token = new RefreshToken
		{
			UserId = 1,
			Token = "token123"
		};

		// Assert
		token.Id.Should().Be(0);
		token.TenantId.Should().BeNull();
		token.IsRevoked.Should().BeFalse();
		token.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
	}

	[Fact]
	public void ExpiredToken_HasExpiresAtInPast()
	{
		// Arrange & Act
		var token = RefreshTokenBuilder.Expired()
			.WithUserId(1)
			.Build();

		// Assert
		token.ExpiresAt.Should().BeBefore(DateTime.UtcNow);
	}

	[Fact]
	public void ValidToken_HasExpiresAtInFuture()
	{
		// Arrange & Act
		var token = RefreshTokenBuilder.Default()
			.WithUserId(1)
			.Build();

		// Assert
		token.ExpiresAt.Should().BeAfter(DateTime.UtcNow);
	}

	[Fact]
	public void RevokedToken_HasIsRevokedTrue()
	{
		// Arrange & Act
		var token = RefreshTokenBuilder.Revoked()
			.WithUserId(1)
			.Build();

		// Assert
		token.IsRevoked.Should().BeTrue();
	}

	[Fact]
	public void RefreshTokenBuilder_WithTenant_SetsTenantId()
	{
		// Arrange
		var tenant = TenantBuilder.Default().WithId(5).Build();

		// Act
		var token = RefreshTokenBuilder.Default()
			.WithUserId(1)
			.WithTenant(tenant)
			.Build();

		// Assert
		token.TenantId.Should().Be(5);
		token.Tenant.Should().Be(tenant);
	}

	[Fact]
	public void SuperAdminToken_HasNoTenant()
	{
		// Arrange
		var superAdmin = UserBuilder.SuperAdmin().WithId(1).Build();

		// Act
		var token = RefreshTokenBuilder.Default()
			.WithUser(superAdmin)
			.WithTenant(null)
			.Build();

		// Assert
		token.TenantId.Should().BeNull();
		token.Tenant.Should().BeNull();
	}
}
