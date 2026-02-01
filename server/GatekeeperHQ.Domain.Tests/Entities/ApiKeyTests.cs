using System.Text.Json;
using FluentAssertions;
using GatekeeperHQ.Domain.Entities;
using GatekeeperHQ.Tests.Common.Builders;
using Xunit;

namespace GatekeeperHQ.Domain.Tests.Entities;

[Trait("Category", "Unit")]
public class ApiKeyTests
{
	[Fact]
	public void NewApiKey_HasCorrectDefaults()
	{
		// Arrange & Act
		var apiKey = new ApiKey
		{
			TenantId = 1,
			Name = "Test Key",
			Key = "key123",
			KeyHash = "hash123"
		};

		// Assert
		apiKey.Id.Should().Be(0);
		apiKey.IsActive.Should().BeTrue();
		apiKey.ExpiresAt.Should().BeNull();
		apiKey.LastUsedAt.Should().BeNull();
		apiKey.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
		apiKey.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
	}

	[Fact]
	public void ExpiredApiKey_HasExpiresAtInPast()
	{
		// Arrange & Act
		var apiKey = ApiKeyBuilder.Expired()
			.WithTenantId(1)
			.Build();

		// Assert
		apiKey.ExpiresAt.Should().NotBeNull();
		apiKey.ExpiresAt.Should().BeBefore(DateTime.UtcNow);
	}

	[Fact]
	public void InactiveApiKey_HasIsActiveFalse()
	{
		// Arrange & Act
		var apiKey = ApiKeyBuilder.Inactive()
			.WithTenantId(1)
			.Build();

		// Assert
		apiKey.IsActive.Should().BeFalse();
	}

	[Fact]
	public void ApiKeyBuilder_WithPermissions_StoresAsJson()
	{
		// Arrange & Act
		var apiKey = ApiKeyBuilder.Default()
			.WithTenantId(1)
			.WithPermissions("users.view", "roles.view")
			.Build();

		// Assert
		var permissions = JsonSerializer.Deserialize<List<string>>(apiKey.Permissions);
		permissions.Should().NotBeNull();
		permissions.Should().HaveCount(2);
		permissions.Should().Contain(new[] { "users.view", "roles.view" });
	}

	[Fact]
	public void ApiKeyBuilder_WithKey_ComputesHash()
	{
		// Arrange
		const string key = "test-api-key-123";

		// Act
		var apiKey = ApiKeyBuilder.Default()
			.WithTenantId(1)
			.WithKey(key)
			.Build();

		// Assert
		apiKey.Key.Should().Be(key);
		apiKey.KeyHash.Should().NotBe(key);
		apiKey.KeyHash.Should().NotBeNullOrEmpty();
		apiKey.KeyHash.Should().HaveLength(64);  // SHA256 hex string length
	}

	[Fact]
	public void ApiKeyBuilder_GeneratesUniqueKeys()
	{
		// Arrange & Act
		var apiKey1 = ApiKeyBuilder.Default().WithTenantId(1).Build();
		var apiKey2 = ApiKeyBuilder.Default().WithTenantId(1).Build();

		// Assert
		apiKey1.Key.Should().NotBe(apiKey2.Key);
		apiKey1.KeyHash.Should().NotBe(apiKey2.KeyHash);
	}

	[Fact]
	public void ApiKeyBuilder_WithLastUsedAt_SetsTimestamp()
	{
		// Arrange
		var lastUsed = DateTime.UtcNow.AddHours(-1);

		// Act
		var apiKey = ApiKeyBuilder.Default()
			.WithTenantId(1)
			.WithLastUsedAt(lastUsed)
			.Build();

		// Assert
		apiKey.LastUsedAt.Should().Be(lastUsed);
	}

	[Fact]
	public void ApiKeyBuilder_WithExpiresAt_SetsExpiration()
	{
		// Arrange
		var expiresAt = DateTime.UtcNow.AddDays(30);

		// Act
		var apiKey = ApiKeyBuilder.Default()
			.WithTenantId(1)
			.WithExpiresAt(expiresAt)
			.Build();

		// Assert
		apiKey.ExpiresAt.Should().Be(expiresAt);
	}
}
