using System.Text.Json;
using Bogus;
using GatekeeperHQ.Domain.Entities;

namespace GatekeeperHQ.Tests.Common.Builders;

public class WebhookBuilder
{
	private readonly Faker _faker = new();
	private int? _id;
	private int _tenantId;
	private Tenant? _tenant;
	private string? _url;
	private string? _secret;
	private List<string> _events = new();
	private bool _isActive = true;

	public WebhookBuilder WithId(int id)
	{
		_id = id;
		return this;
	}

	public WebhookBuilder WithTenantId(int tenantId)
	{
		_tenantId = tenantId;
		return this;
	}

	public WebhookBuilder WithTenant(Tenant tenant)
	{
		_tenant = tenant;
		_tenantId = tenant.Id;
		return this;
	}

	public WebhookBuilder WithUrl(string url)
	{
		_url = url;
		return this;
	}

	public WebhookBuilder WithSecret(string secret)
	{
		_secret = secret;
		return this;
	}

	public WebhookBuilder WithEvents(params string[] events)
	{
		_events = events.ToList();
		return this;
	}

	public WebhookBuilder AsInactive()
	{
		_isActive = false;
		return this;
	}

	public Webhook Build()
	{
		var now = DateTime.UtcNow;
		return new Webhook
		{
			Id = _id ?? 0,
			TenantId = _tenantId,
			Tenant = _tenant!,
			Url = _url ?? _faker.Internet.Url(),
			Secret = _secret ?? Guid.NewGuid().ToString("N"),
			Events = JsonSerializer.Serialize(_events),
			IsActive = _isActive,
			CreatedAt = now,
			UpdatedAt = now
		};
	}

	public static WebhookBuilder Default() => new();
	public static WebhookBuilder Inactive() => new WebhookBuilder().AsInactive();
}
