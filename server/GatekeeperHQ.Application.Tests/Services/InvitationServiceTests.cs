using FluentAssertions;
using GatekeeperHQ.Application.Services;
using GatekeeperHQ.Tests.Common.Builders;
using GatekeeperHQ.Tests.Common.Fixtures;
using Xunit;

namespace GatekeeperHQ.Application.Tests.Services;

[Trait("Category", "Unit")]
public class InvitationServiceTests : IDisposable
{
	private readonly Infrastructure.Data.AppDbContext _context;
	private readonly InvitationService _invitationService;

	public InvitationServiceTests()
	{
		_context = InMemoryDbContextFactory.Create();
		_invitationService = new InvitationService(_context);
	}

	public void Dispose()
	{
		_context.Dispose();
	}

	[Fact]
	public async Task CreateInvitationAsync_CreatesInvitation()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		var creator = UserBuilder.SuperAdmin().Build();
		_context.Users.Add(creator);
		await _context.SaveChangesAsync();

		var request = new CreateInvitationRequest { Email = "invite@test.com" };

		// Act
		var result = await _invitationService.CreateInvitationAsync(tenant.Id, request, creator.Id);

		// Assert
		result.Should().NotBeNull();
		result.Email.Should().Be("invite@test.com");
		result.Token.Should().NotBeNullOrEmpty();
		result.Status.Should().Be("pending");
	}

	[Fact]
	public async Task CreateInvitationAsync_LowercasesEmail()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		var creator = UserBuilder.SuperAdmin().Build();
		_context.Users.Add(creator);
		await _context.SaveChangesAsync();

		var request = new CreateInvitationRequest { Email = "UPPER@TEST.COM" };

		// Act
		var result = await _invitationService.CreateInvitationAsync(tenant.Id, request, creator.Id);

		// Assert
		result.Email.Should().Be("upper@test.com");
	}

	[Fact]
	public async Task CreateInvitationAsync_ThrowsException_WhenTenantNotFound()
	{
		// Arrange
		var creator = UserBuilder.SuperAdmin().Build();
		_context.Users.Add(creator);
		await _context.SaveChangesAsync();

		var request = new CreateInvitationRequest { Email = "test@test.com" };

		// Act & Assert
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => _invitationService.CreateInvitationAsync(999, request, creator.Id));
		exception.Message.Should().Be("Tenant not found");
	}

	[Fact]
	public async Task CreateInvitationAsync_ThrowsException_WhenPendingInvitationExists()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		var creator = UserBuilder.SuperAdmin().Build();
		_context.Users.Add(creator);
		await _context.SaveChangesAsync();

		var existingInvitation = InvitationBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithEmail("existing@test.com")
			.WithCreatedByUserId(creator.Id)
			.Build();
		_context.Invitations.Add(existingInvitation);
		await _context.SaveChangesAsync();

		var request = new CreateInvitationRequest { Email = "existing@test.com" };

		// Act & Assert
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => _invitationService.CreateInvitationAsync(tenant.Id, request, creator.Id));
		exception.Message.Should().Contain("already exists");
	}

	[Fact]
	public async Task CreateInvitationAsync_ThrowsException_WhenUserAlreadyExists()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		var creator = UserBuilder.SuperAdmin().Build();
		var existingUser = UserBuilder.Default().WithTenantId(tenant.Id).WithEmail("existing@test.com").Build();
		_context.Users.AddRange(creator, existingUser);
		await _context.SaveChangesAsync();

		var request = new CreateInvitationRequest { Email = "existing@test.com" };

		// Act & Assert
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => _invitationService.CreateInvitationAsync(tenant.Id, request, creator.Id));
		exception.Message.Should().Contain("user with this email already exists");
	}

	[Fact]
	public async Task GetInvitationByTokenAsync_ReturnsInvitation()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		var creator = UserBuilder.SuperAdmin().Build();
		_context.Users.Add(creator);
		await _context.SaveChangesAsync();

		var invitation = InvitationBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithEmail("test@test.com")
			.WithToken("test-token")
			.WithCreatedByUserId(creator.Id)
			.Build();
		_context.Invitations.Add(invitation);
		await _context.SaveChangesAsync();

		// Act
		var result = await _invitationService.GetInvitationByTokenAsync("test-token");

		// Assert
		result.Should().NotBeNull();
		result!.Email.Should().Be("test@test.com");
	}

	[Fact]
	public async Task GetInvitationByTokenAsync_ReturnsNull_WhenNotFound()
	{
		// Act
		var result = await _invitationService.GetInvitationByTokenAsync("nonexistent");

		// Assert
		result.Should().BeNull();
	}

	[Fact]
	public async Task AcceptInvitationAsync_CreatesUser()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		var creator = UserBuilder.SuperAdmin().Build();
		_context.Users.Add(creator);
		await _context.SaveChangesAsync();

		var invitation = InvitationBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithEmail("newuser@test.com")
			.WithToken("accept-token")
			.WithCreatedByUserId(creator.Id)
			.Build();
		_context.Invitations.Add(invitation);
		await _context.SaveChangesAsync();

		// Act
		var result = await _invitationService.AcceptInvitationAsync("accept-token", "Password123!");

		// Assert
		result.Success.Should().BeTrue();
		result.UserId.Should().NotBeNull();
		result.Email.Should().Be("newuser@test.com");

		var user = await _context.Users.FindAsync(result.UserId);
		user.Should().NotBeNull();
		user!.TenantId.Should().Be(tenant.Id);
	}

	[Fact]
	public async Task AcceptInvitationAsync_AssignsRole_WhenSpecified()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		var creator = UserBuilder.SuperAdmin().Build();
		_context.Users.Add(creator);
		await _context.SaveChangesAsync();

		var role = RoleBuilder.Default().WithTenantId(tenant.Id).WithName("Admin").Build();
		_context.Roles.Add(role);
		await _context.SaveChangesAsync();

		var invitation = InvitationBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithEmail("newuser@test.com")
			.WithToken("accept-with-role")
			.WithCreatedByUserId(creator.Id)
			.Build();
		invitation.RoleId = role.Id;
		_context.Invitations.Add(invitation);
		await _context.SaveChangesAsync();

		// Act
		var result = await _invitationService.AcceptInvitationAsync("accept-with-role", "Password123!");

		// Assert
		result.Success.Should().BeTrue();
		var userRoles = _context.UserRoles.Where(ur => ur.UserId == result.UserId).ToList();
		userRoles.Should().HaveCount(1);
		userRoles[0].RoleId.Should().Be(role.Id);
	}

	[Fact]
	public async Task AcceptInvitationAsync_MarksInvitationAsAccepted()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		var creator = UserBuilder.SuperAdmin().Build();
		_context.Users.Add(creator);
		await _context.SaveChangesAsync();

		var invitation = InvitationBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithEmail("newuser@test.com")
			.WithToken("mark-accepted")
			.WithCreatedByUserId(creator.Id)
			.Build();
		_context.Invitations.Add(invitation);
		await _context.SaveChangesAsync();

		// Act
		await _invitationService.AcceptInvitationAsync("mark-accepted", "Password123!");

		// Assert
		await _context.Entry(invitation).ReloadAsync();
		invitation.AcceptedAt.Should().NotBeNull();
		invitation.IsAccepted.Should().BeTrue();
	}

	[Fact]
	public async Task AcceptInvitationAsync_ReturnsError_WhenExpired()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		var creator = UserBuilder.SuperAdmin().Build();
		_context.Users.Add(creator);
		await _context.SaveChangesAsync();

		var invitation = InvitationBuilder.Expired()
			.WithTenantId(tenant.Id)
			.WithEmail("expired@test.com")
			.WithToken("expired-token")
			.WithCreatedByUserId(creator.Id)
			.Build();
		_context.Invitations.Add(invitation);
		await _context.SaveChangesAsync();

		// Act
		var result = await _invitationService.AcceptInvitationAsync("expired-token", "Password123!");

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("expired");
	}

	[Fact]
	public async Task AcceptInvitationAsync_ReturnsError_WhenAlreadyAccepted()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		var creator = UserBuilder.SuperAdmin().Build();
		_context.Users.Add(creator);
		await _context.SaveChangesAsync();

		var invitation = InvitationBuilder.Accepted()
			.WithTenantId(tenant.Id)
			.WithEmail("accepted@test.com")
			.WithToken("already-accepted")
			.WithCreatedByUserId(creator.Id)
			.Build();
		_context.Invitations.Add(invitation);
		await _context.SaveChangesAsync();

		// Act
		var result = await _invitationService.AcceptInvitationAsync("already-accepted", "Password123!");

		// Assert
		result.Success.Should().BeFalse();
		result.Error.Should().Contain("already been accepted");
	}

	[Fact]
	public async Task RevokeInvitationAsync_DeletesInvitation()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		var creator = UserBuilder.SuperAdmin().Build();
		_context.Users.Add(creator);
		await _context.SaveChangesAsync();

		var invitation = InvitationBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithCreatedByUserId(creator.Id)
			.Build();
		_context.Invitations.Add(invitation);
		await _context.SaveChangesAsync();

		var invitationId = invitation.Id;

		// Act
		var result = await _invitationService.RevokeInvitationAsync(invitationId);

		// Assert
		result.Should().BeTrue();
		var dbInvitation = await _context.Invitations.FindAsync(invitationId);
		dbInvitation.Should().BeNull();
	}

	[Fact]
	public async Task RevokeInvitationAsync_ThrowsException_WhenAlreadyAccepted()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		var creator = UserBuilder.SuperAdmin().Build();
		_context.Users.Add(creator);
		await _context.SaveChangesAsync();

		var invitation = InvitationBuilder.Accepted()
			.WithTenantId(tenant.Id)
			.WithCreatedByUserId(creator.Id)
			.Build();
		_context.Invitations.Add(invitation);
		await _context.SaveChangesAsync();

		// Act & Assert
		var exception = await Assert.ThrowsAsync<InvalidOperationException>(
			() => _invitationService.RevokeInvitationAsync(invitation.Id));
		exception.Message.Should().Contain("Cannot revoke");
	}

	[Fact]
	public async Task GetTenantInvitationsAsync_ReturnsAllTenantInvitations()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		var creator = UserBuilder.SuperAdmin().Build();
		_context.Users.Add(creator);
		await _context.SaveChangesAsync();

		var invitation1 = InvitationBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithEmail("inv1@test.com")
			.WithCreatedByUserId(creator.Id)
			.Build();
		var invitation2 = InvitationBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithEmail("inv2@test.com")
			.WithCreatedByUserId(creator.Id)
			.Build();
		_context.Invitations.AddRange(invitation1, invitation2);
		await _context.SaveChangesAsync();

		// Act
		var result = await _invitationService.GetTenantInvitationsAsync(tenant.Id);

		// Assert
		result.Should().HaveCount(2);
	}
}
