using FluentAssertions;
using GatekeeperHQ.Application.Services;
using GatekeeperHQ.Domain.Entities;
using GatekeeperHQ.Infrastructure.Webhooks;
using GatekeeperHQ.Tests.Common.Builders;
using GatekeeperHQ.Tests.Common.Fixtures;
using Moq;
using Xunit;

namespace GatekeeperHQ.Application.Tests.Services;

[Trait("Category", "Unit")]
public class WebhookServiceTests : IDisposable
{
	private readonly Infrastructure.Data.AppDbContext _context;
	private readonly MockTenantContext _tenantContext;
	private readonly Mock<IWebhookDispatcher> _webhookDispatcher;
	private readonly WebhookService _webhookService;

	public WebhookServiceTests()
	{
		_context = InMemoryDbContextFactory.Create();
		_tenantContext = new MockTenantContext();
		_webhookDispatcher = new Mock<IWebhookDispatcher>();
		_webhookService = new WebhookService(_context, _tenantContext, _webhookDispatcher.Object);
	}

	public void Dispose()
	{
		_context.Dispose();
	}

	[Fact]
	public async Task GetAllWebhooksAsync_WithoutTenantContext_ThrowsInvalidOperationException()
	{
		// Act & Assert
		await Assert.ThrowsAsync<InvalidOperationException>(() => _webhookService.GetAllWebhooksAsync());
	}

	[Fact]
	public async Task GetAllWebhooksAsync_ReturnsTenantWebhooks()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var webhook1 = WebhookBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithUrl("https://example.com/hook1")
			.Build();
		var webhook2 = WebhookBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithUrl("https://example.com/hook2")
			.Build();
		_context.Webhooks.AddRange(webhook1, webhook2);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		// Act
		var result = await _webhookService.GetAllWebhooksAsync();

		// Assert
		result.Should().HaveCount(2);
	}

	[Fact]
	public async Task CreateWebhookAsync_CreatesWebhook()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		_tenantContext.SetTenantId(tenant.Id);

		var request = new CreateWebhookRequest
		{
			Url = "https://example.com/webhook",
			Events = new List<string> { WebhookEvents.UserCreated },
			IsActive = true
		};

		// Act
		var result = await _webhookService.CreateWebhookAsync(request);

		// Assert
		result.Should().NotBeNull();
		result.Url.Should().Be("https://example.com/webhook");
		result.Events.Should().Contain(WebhookEvents.UserCreated);
	}

	[Fact]
	public async Task CreateWebhookAsync_GeneratesSecret_WhenNotProvided()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		_tenantContext.SetTenantId(tenant.Id);

		var request = new CreateWebhookRequest
		{
			Url = "https://example.com/webhook",
			Events = new List<string>()
		};

		// Act
		await _webhookService.CreateWebhookAsync(request);

		// Assert
		var dbWebhook = _context.Webhooks.First();
		dbWebhook.Secret.Should().NotBeNullOrEmpty();
	}

	[Fact]
	public async Task CreateWebhookAsync_UsesProvidedSecret()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();
		_tenantContext.SetTenantId(tenant.Id);

		var request = new CreateWebhookRequest
		{
			Url = "https://example.com/webhook",
			Secret = "my-custom-secret",
			Events = new List<string>()
		};

		// Act
		await _webhookService.CreateWebhookAsync(request);

		// Assert
		var dbWebhook = _context.Webhooks.First();
		dbWebhook.Secret.Should().Be("my-custom-secret");
	}

	[Fact]
	public async Task UpdateWebhookAsync_UpdatesUrl()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var webhook = WebhookBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithUrl("https://old.example.com")
			.Build();
		_context.Webhooks.Add(webhook);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		var request = new UpdateWebhookRequest { Url = "https://new.example.com" };

		// Act
		var result = await _webhookService.UpdateWebhookAsync(webhook.Id, request);

		// Assert
		result.Should().NotBeNull();
		result!.Url.Should().Be("https://new.example.com");
	}

	[Fact]
	public async Task UpdateWebhookAsync_UpdatesEvents()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var webhook = WebhookBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithEvents(WebhookEvents.UserCreated)
			.Build();
		_context.Webhooks.Add(webhook);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		var request = new UpdateWebhookRequest
		{
			Events = new List<string> { WebhookEvents.UserUpdated, WebhookEvents.UserDeleted }
		};

		// Act
		var result = await _webhookService.UpdateWebhookAsync(webhook.Id, request);

		// Assert
		result.Should().NotBeNull();
		result!.Events.Should().HaveCount(2);
		result.Events.Should().Contain(WebhookEvents.UserUpdated);
		result.Events.Should().Contain(WebhookEvents.UserDeleted);
	}

	[Fact]
	public async Task DeleteWebhookAsync_DeletesWebhook()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var webhook = WebhookBuilder.Default().WithTenantId(tenant.Id).Build();
		_context.Webhooks.Add(webhook);
		await _context.SaveChangesAsync();

		var webhookId = webhook.Id;
		_tenantContext.SetTenantId(tenant.Id);

		// Act
		var result = await _webhookService.DeleteWebhookAsync(webhookId);

		// Assert
		result.Should().BeTrue();
		var dbWebhook = await _context.Webhooks.FindAsync(webhookId);
		dbWebhook.Should().BeNull();
	}

	[Fact]
	public async Task TriggerWebhookAsync_DispatchesToSubscribedWebhooks()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var webhook = WebhookBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithEvents(WebhookEvents.UserCreated)
			.Build();
		_context.Webhooks.Add(webhook);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		_webhookDispatcher
			.Setup(d => d.DispatchAsync(It.IsAny<Webhook>(), It.IsAny<string>(), It.IsAny<object>()))
			.Returns(Task.CompletedTask);

		// Act
		await _webhookService.TriggerWebhookAsync(WebhookEvents.UserCreated, new { userId = 1 });

		// Assert
		_webhookDispatcher.Verify(
			d => d.DispatchAsync(
				It.Is<Webhook>(w => w.Id == webhook.Id),
				WebhookEvents.UserCreated,
				It.IsAny<object>()),
			Times.Once);
	}

	[Fact]
	public async Task TriggerWebhookAsync_DoesNotDispatchToUnsubscribedWebhooks()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var webhook = WebhookBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithEvents(WebhookEvents.UserCreated) // Only subscribed to UserCreated
			.Build();
		_context.Webhooks.Add(webhook);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		// Act
		await _webhookService.TriggerWebhookAsync(WebhookEvents.RoleCreated, new { roleId = 1 });

		// Assert
		_webhookDispatcher.Verify(
			d => d.DispatchAsync(It.IsAny<Webhook>(), It.IsAny<string>(), It.IsAny<object>()),
			Times.Never);
	}

	[Fact]
	public async Task TriggerWebhookAsync_DoesNotDispatchToInactiveWebhooks()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var webhook = WebhookBuilder.Inactive()
			.WithTenantId(tenant.Id)
			.WithEvents(WebhookEvents.UserCreated)
			.Build();
		_context.Webhooks.Add(webhook);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		// Act
		await _webhookService.TriggerWebhookAsync(WebhookEvents.UserCreated, new { userId = 1 });

		// Assert
		_webhookDispatcher.Verify(
			d => d.DispatchAsync(It.IsAny<Webhook>(), It.IsAny<string>(), It.IsAny<object>()),
			Times.Never);
	}

	[Fact]
	public async Task TriggerWebhookAsync_WithoutTenantContext_DoesNothing()
	{
		// Arrange - no tenant context set

		// Act
		await _webhookService.TriggerWebhookAsync(WebhookEvents.UserCreated, new { userId = 1 });

		// Assert - no exception and no dispatch
		_webhookDispatcher.Verify(
			d => d.DispatchAsync(It.IsAny<Webhook>(), It.IsAny<string>(), It.IsAny<object>()),
			Times.Never);
	}

	[Fact]
	public async Task GetWebhookByIdAsync_ReturnsWebhook()
	{
		// Arrange
		var tenant = TenantBuilder.Default().Build();
		_context.Tenants.Add(tenant);
		await _context.SaveChangesAsync();

		var webhook = WebhookBuilder.Default()
			.WithTenantId(tenant.Id)
			.WithUrl("https://test.com")
			.Build();
		_context.Webhooks.Add(webhook);
		await _context.SaveChangesAsync();

		_tenantContext.SetTenantId(tenant.Id);

		// Act
		var result = await _webhookService.GetWebhookByIdAsync(webhook.Id);

		// Assert
		result.Should().NotBeNull();
		result!.Url.Should().Be("https://test.com");
	}
}
