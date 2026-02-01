using Bogus;
using GatekeeperHQ.Domain.Entities;

namespace GatekeeperHQ.Tests.Common.Builders;

public class UserBuilder
{
	private readonly Faker _faker = new();
	private int? _id;
	private int? _tenantId;
	private Tenant? _tenant;
	private string? _email;
	private string? _passwordHash;
	private bool _isActive = true;
	private bool _isSuperAdmin;
	private DateTime? _createdAt;
	private DateTime? _updatedAt;
	private List<UserRole> _userRoles = new();

	public UserBuilder WithId(int id)
	{
		_id = id;
		return this;
	}

	public UserBuilder WithTenantId(int tenantId)
	{
		_tenantId = tenantId;
		return this;
	}

	public UserBuilder WithTenant(Tenant tenant)
	{
		_tenant = tenant;
		_tenantId = tenant.Id;
		return this;
	}

	public UserBuilder WithEmail(string email)
	{
		_email = email;
		return this;
	}

	public UserBuilder WithPassword(string password)
	{
		_passwordHash = BCrypt.Net.BCrypt.HashPassword(password);
		return this;
	}

	public UserBuilder WithPasswordHash(string passwordHash)
	{
		_passwordHash = passwordHash;
		return this;
	}

	public UserBuilder AsInactive()
	{
		_isActive = false;
		return this;
	}

	public UserBuilder AsSuperAdmin()
	{
		_isSuperAdmin = true;
		_tenantId = null;
		_tenant = null;
		return this;
	}

	public UserBuilder WithRole(Role role)
	{
		_userRoles.Add(new UserRole { RoleId = role.Id, Role = role });
		return this;
	}

	public UserBuilder WithCreatedAt(DateTime createdAt)
	{
		_createdAt = createdAt;
		return this;
	}

	public User Build()
	{
		var now = DateTime.UtcNow;
		var user = new User
		{
			Id = _id ?? 0,
			TenantId = _tenantId,
			Tenant = _tenant,
			Email = _email ?? _faker.Internet.Email(),
			PasswordHash = _passwordHash ?? BCrypt.Net.BCrypt.HashPassword("Test123!"),
			IsActive = _isActive,
			IsSuperAdmin = _isSuperAdmin,
			CreatedAt = _createdAt ?? now,
			UpdatedAt = _updatedAt ?? now
		};

		foreach (var userRole in _userRoles)
		{
			userRole.UserId = user.Id;
			userRole.User = user;
			user.UserRoles.Add(userRole);
		}

		return user;
	}

	public static UserBuilder Default() => new();
	public static UserBuilder SuperAdmin() => new UserBuilder().AsSuperAdmin();
}
