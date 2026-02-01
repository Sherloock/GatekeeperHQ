using Bogus;
using GatekeeperHQ.Domain.Entities;

namespace GatekeeperHQ.Tests.Common.Builders;

public class TenantBuilder
{
	private readonly Faker _faker = new();
	private int? _id;
	private string? _name;
	private string? _apiKey;
	private bool _isActive = true;
	private DateTime? _createdAt;
	private DateTime? _updatedAt;

	public TenantBuilder WithId(int id)
	{
		_id = id;
		return this;
	}

	public TenantBuilder WithName(string name)
	{
		_name = name;
		return this;
	}

	public TenantBuilder WithApiKey(string apiKey)
	{
		_apiKey = apiKey;
		return this;
	}

	public TenantBuilder AsInactive()
	{
		_isActive = false;
		return this;
	}

	public TenantBuilder WithCreatedAt(DateTime createdAt)
	{
		_createdAt = createdAt;
		return this;
	}

	public Tenant Build()
	{
		var now = DateTime.UtcNow;
		return new Tenant
		{
			Id = _id ?? 0,
			Name = _name ?? _faker.Company.CompanyName(),
			ApiKey = _apiKey ?? Guid.NewGuid().ToString("N"),
			IsActive = _isActive,
			CreatedAt = _createdAt ?? now,
			UpdatedAt = _updatedAt ?? now
		};
	}

	public static TenantBuilder Default() => new();
}
