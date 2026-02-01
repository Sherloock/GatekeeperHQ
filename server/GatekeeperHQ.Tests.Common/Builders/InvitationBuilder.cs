using Bogus;
using GatekeeperHQ.Domain.Entities;

namespace GatekeeperHQ.Tests.Common.Builders;

public class InvitationBuilder
{
	private readonly Faker _faker = new();
	private int? _id;
	private int _tenantId;
	private Tenant? _tenant;
	private string? _email;
	private string? _token;
	private int? _roleId;
	private Role? _role;
	private DateTime? _expiresAt;
	private DateTime? _acceptedAt;
	private int _createdByUserId;
	private User? _createdBy;
	private DateTime? _createdAt;

	public InvitationBuilder WithId(int id)
	{
		_id = id;
		return this;
	}

	public InvitationBuilder WithTenantId(int tenantId)
	{
		_tenantId = tenantId;
		return this;
	}

	public InvitationBuilder WithTenant(Tenant tenant)
	{
		_tenant = tenant;
		_tenantId = tenant.Id;
		return this;
	}

	public InvitationBuilder WithEmail(string email)
	{
		_email = email;
		return this;
	}

	public InvitationBuilder WithToken(string token)
	{
		_token = token;
		return this;
	}

	public InvitationBuilder WithRole(Role role)
	{
		_role = role;
		_roleId = role.Id;
		return this;
	}

	public InvitationBuilder WithExpiresAt(DateTime expiresAt)
	{
		_expiresAt = expiresAt;
		return this;
	}

	public InvitationBuilder AsExpired()
	{
		_expiresAt = DateTime.UtcNow.AddDays(-1);
		return this;
	}

	public InvitationBuilder AsAccepted()
	{
		_acceptedAt = DateTime.UtcNow;
		return this;
	}

	public InvitationBuilder WithCreatedBy(User user)
	{
		_createdBy = user;
		_createdByUserId = user.Id;
		return this;
	}

	public InvitationBuilder WithCreatedByUserId(int userId)
	{
		_createdByUserId = userId;
		return this;
	}

	public Invitation Build()
	{
		return new Invitation
		{
			Id = _id ?? 0,
			TenantId = _tenantId,
			Tenant = _tenant!,
			Email = _email ?? _faker.Internet.Email(),
			Token = _token ?? Guid.NewGuid().ToString("N"),
			RoleId = _roleId,
			Role = _role,
			ExpiresAt = _expiresAt ?? DateTime.UtcNow.AddDays(7),
			AcceptedAt = _acceptedAt,
			CreatedByUserId = _createdByUserId,
			CreatedBy = _createdBy!,
			CreatedAt = _createdAt ?? DateTime.UtcNow
		};
	}

	public static InvitationBuilder Default() => new();
	public static InvitationBuilder Expired() => new InvitationBuilder().AsExpired();
	public static InvitationBuilder Accepted() => new InvitationBuilder().AsAccepted();
}
