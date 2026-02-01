using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Bogus;
using GatekeeperHQ.Domain.Entities;

namespace GatekeeperHQ.Tests.Common.Builders;

public class ApiKeyBuilder
{
	private readonly Faker _faker = new();
	private int? _id;
	private int _tenantId;
	private Tenant? _tenant;
	private string? _name;
	private string? _key;
	private string? _keyHash;
	private List<string> _permissions = new();
	private bool _isActive = true;
	private DateTime? _expiresAt;
	private DateTime? _lastUsedAt;

	public ApiKeyBuilder WithId(int id)
	{
		_id = id;
		return this;
	}

	public ApiKeyBuilder WithTenantId(int tenantId)
	{
		_tenantId = tenantId;
		return this;
	}

	public ApiKeyBuilder WithTenant(Tenant tenant)
	{
		_tenant = tenant;
		_tenantId = tenant.Id;
		return this;
	}

	public ApiKeyBuilder WithName(string name)
	{
		_name = name;
		return this;
	}

	public ApiKeyBuilder WithKey(string key)
	{
		_key = key;
		_keyHash = ComputeHash(key);
		return this;
	}

	public ApiKeyBuilder WithPermissions(params string[] permissions)
	{
		_permissions = permissions.ToList();
		return this;
	}

	public ApiKeyBuilder AsInactive()
	{
		_isActive = false;
		return this;
	}

	public ApiKeyBuilder WithExpiresAt(DateTime? expiresAt)
	{
		_expiresAt = expiresAt;
		return this;
	}

	public ApiKeyBuilder AsExpired()
	{
		_expiresAt = DateTime.UtcNow.AddDays(-1);
		return this;
	}

	public ApiKeyBuilder WithLastUsedAt(DateTime lastUsedAt)
	{
		_lastUsedAt = lastUsedAt;
		return this;
	}

	public ApiKey Build()
	{
		var now = DateTime.UtcNow;
		var key = _key ?? Guid.NewGuid().ToString("N");

		return new ApiKey
		{
			Id = _id ?? 0,
			TenantId = _tenantId,
			Tenant = _tenant!,
			Name = _name ?? _faker.Lorem.Word(),
			Key = key,
			KeyHash = _keyHash ?? ComputeHash(key),
			Permissions = JsonSerializer.Serialize(_permissions),
			IsActive = _isActive,
			ExpiresAt = _expiresAt,
			CreatedAt = now,
			UpdatedAt = now,
			LastUsedAt = _lastUsedAt
		};
	}

	private static string ComputeHash(string input)
	{
		var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
		return Convert.ToHexString(bytes).ToLowerInvariant();
	}

	public static ApiKeyBuilder Default() => new();
	public static ApiKeyBuilder Expired() => new ApiKeyBuilder().AsExpired();
	public static ApiKeyBuilder Inactive() => new ApiKeyBuilder().AsInactive();
}
