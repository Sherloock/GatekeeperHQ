using FluentAssertions;
using GatekeeperHQ.Domain.Entities;
using GatekeeperHQ.Tests.Common.Builders;
using Xunit;

namespace GatekeeperHQ.Domain.Tests.Entities;

[Trait("Category", "Unit")]
public class InvitationTests
{
	[Fact]
	public void IsExpired_WhenExpiresAtIsInThePast_ReturnsTrue()
	{
		// Arrange
		var invitation = InvitationBuilder.Default()
			.WithExpiresAt(DateTime.UtcNow.AddHours(-1))
			.Build();

		// Act & Assert
		invitation.IsExpired.Should().BeTrue();
	}

	[Fact]
	public void IsExpired_WhenExpiresAtIsInTheFuture_ReturnsFalse()
	{
		// Arrange
		var invitation = InvitationBuilder.Default()
			.WithExpiresAt(DateTime.UtcNow.AddDays(7))
			.Build();

		// Act & Assert
		invitation.IsExpired.Should().BeFalse();
	}

	[Fact]
	public void IsAccepted_WhenAcceptedAtHasValue_ReturnsTrue()
	{
		// Arrange
		var invitation = InvitationBuilder.Default()
			.AsAccepted()
			.Build();

		// Act & Assert
		invitation.IsAccepted.Should().BeTrue();
	}

	[Fact]
	public void IsAccepted_WhenAcceptedAtIsNull_ReturnsFalse()
	{
		// Arrange
		var invitation = InvitationBuilder.Default()
			.Build();

		// Act & Assert
		invitation.IsAccepted.Should().BeFalse();
	}

	[Fact]
	public void IsPending_WhenNotExpiredAndNotAccepted_ReturnsTrue()
	{
		// Arrange
		var invitation = InvitationBuilder.Default()
			.WithExpiresAt(DateTime.UtcNow.AddDays(7))
			.Build();

		// Act & Assert
		invitation.IsPending.Should().BeTrue();
	}

	[Fact]
	public void IsPending_WhenExpired_ReturnsFalse()
	{
		// Arrange
		var invitation = InvitationBuilder.Expired()
			.Build();

		// Act & Assert
		invitation.IsPending.Should().BeFalse();
	}

	[Fact]
	public void IsPending_WhenAccepted_ReturnsFalse()
	{
		// Arrange
		var invitation = InvitationBuilder.Accepted()
			.Build();

		// Act & Assert
		invitation.IsPending.Should().BeFalse();
	}

	[Fact]
	public void IsPending_WhenExpiredAndAccepted_ReturnsFalse()
	{
		// Arrange
		var invitation = InvitationBuilder.Default()
			.AsExpired()
			.AsAccepted()
			.Build();

		// Act & Assert
		invitation.IsPending.Should().BeFalse();
	}

	[Fact]
	public void NewInvitation_HasCorrectDefaults()
	{
		// Arrange & Act
		var invitation = new Invitation
		{
			TenantId = 1,
			Email = "test@example.com",
			Token = "token123",
			CreatedByUserId = 1
		};

		// Assert
		invitation.Id.Should().Be(0);
		invitation.RoleId.Should().BeNull();
		invitation.AcceptedAt.Should().BeNull();
		invitation.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
	}
}
