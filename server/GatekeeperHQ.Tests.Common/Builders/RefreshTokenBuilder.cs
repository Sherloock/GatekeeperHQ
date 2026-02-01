using GatekeeperHQ.Domain.Entities;

namespace GatekeeperHQ.Tests.Common.Builders;

public class RefreshTokenBuilder
{
	private int? _id;
	private int _userId;
	private User? _user;
	private int? _tenantId;
	private Tenant? _tenant;
	private string? _token;
	private DateTime? _expiresAt;
	private bool _isRevoked;

	public RefreshTokenBuilder WithId(int id)
	{
		_id = id;
		return this;
	}

	public RefreshTokenBuilder WithUserId(int userId)
	{
		_userId = userId;
		return this;
	}

	public RefreshTokenBuilder WithUser(User user)
	{
		_user = user;
		_userId = user.Id;
		return this;
	}

	public RefreshTokenBuilder WithTenantId(int? tenantId)
	{
		_tenantId = tenantId;
		return this;
	}

	public RefreshTokenBuilder WithTenant(Tenant? tenant)
	{
		_tenant = tenant;
		_tenantId = tenant?.Id;
		return this;
	}

	public RefreshTokenBuilder WithToken(string token)
	{
		_token = token;
		return this;
	}

	public RefreshTokenBuilder WithExpiresAt(DateTime expiresAt)
	{
		_expiresAt = expiresAt;
		return this;
	}

	public RefreshTokenBuilder AsExpired()
	{
		_expiresAt = DateTime.UtcNow.AddDays(-1);
		return this;
	}

	public RefreshTokenBuilder AsRevoked()
	{
		_isRevoked = true;
		return this;
	}

	public RefreshToken Build()
	{
		return new RefreshToken
		{
			Id = _id ?? 0,
			UserId = _userId,
			User = _user!,
			TenantId = _tenantId,
			Tenant = _tenant,
			Token = _token ?? Convert.ToBase64String(Guid.NewGuid().ToByteArray()),
			ExpiresAt = _expiresAt ?? DateTime.UtcNow.AddDays(7),
			CreatedAt = DateTime.UtcNow,
			IsRevoked = _isRevoked
		};
	}

	public static RefreshTokenBuilder Default() => new();
	public static RefreshTokenBuilder Expired() => new RefreshTokenBuilder().AsExpired();
	public static RefreshTokenBuilder Revoked() => new RefreshTokenBuilder().AsRevoked();
}
