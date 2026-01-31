using GatekeeperHQ.Domain.Entities;
using GatekeeperHQ.Infrastructure.Data;
using GatekeeperHQ.Infrastructure.Webhooks;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GatekeeperHQ.Application.Services;

public interface IWebhookService
{
    Task<List<WebhookDto>> GetAllWebhooksAsync();
    Task<WebhookDto?> GetWebhookByIdAsync(int id);
    Task<WebhookDto> CreateWebhookAsync(CreateWebhookRequest request);
    Task<WebhookDto?> UpdateWebhookAsync(int id, UpdateWebhookRequest request);
    Task<bool> DeleteWebhookAsync(int id);
    Task TriggerWebhookAsync(string eventName, object payload);
}

public class WebhookService : IWebhookService
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IWebhookDispatcher _webhookDispatcher;

    public WebhookService(
        AppDbContext context,
        ITenantContext tenantContext,
        IWebhookDispatcher webhookDispatcher)
    {
        _context = context;
        _tenantContext = tenantContext;
        _webhookDispatcher = webhookDispatcher;
    }

    public async Task<List<WebhookDto>> GetAllWebhooksAsync()
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        var webhooks = await _context.Webhooks
            .Where(w => w.TenantId == _tenantContext.TenantId.Value)
            .OrderBy(w => w.Url)
            .ToListAsync();

        return webhooks.Select(w => new WebhookDto
        {
            Id = w.Id,
            Url = w.Url,
            Events = DeserializeEvents(w.Events),
            IsActive = w.IsActive,
            CreatedAt = w.CreatedAt,
            UpdatedAt = w.UpdatedAt
        }).ToList();
    }

    public async Task<WebhookDto?> GetWebhookByIdAsync(int id)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        var webhook = await _context.Webhooks
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == _tenantContext.TenantId.Value);

        if (webhook == null)
            return null;

        return new WebhookDto
        {
            Id = webhook.Id,
            Url = webhook.Url,
            Events = DeserializeEvents(webhook.Events),
            IsActive = webhook.IsActive,
            CreatedAt = webhook.CreatedAt,
            UpdatedAt = webhook.UpdatedAt
        };
    }

    public async Task<WebhookDto> CreateWebhookAsync(CreateWebhookRequest request)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        var webhook = new Webhook
        {
            TenantId = _tenantContext.TenantId.Value,
            Url = request.Url,
            Secret = request.Secret ?? Guid.NewGuid().ToString("N"),
            Events = JsonSerializer.Serialize(request.Events),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.Webhooks.Add(webhook);
        await _context.SaveChangesAsync();

        return await GetWebhookByIdAsync(webhook.Id) ?? throw new InvalidOperationException("Failed to create webhook");
    }

    public async Task<WebhookDto?> UpdateWebhookAsync(int id, UpdateWebhookRequest request)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        var webhook = await _context.Webhooks
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == _tenantContext.TenantId.Value);

        if (webhook == null)
            return null;

        if (!string.IsNullOrEmpty(request.Url))
        {
            webhook.Url = request.Url;
        }

        if (request.Events != null)
        {
            webhook.Events = JsonSerializer.Serialize(request.Events);
        }

        if (request.Secret != null)
        {
            webhook.Secret = request.Secret;
        }

        if (request.IsActive.HasValue)
        {
            webhook.IsActive = request.IsActive.Value;
        }

        webhook.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetWebhookByIdAsync(id);
    }

    public async Task<bool> DeleteWebhookAsync(int id)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        var webhook = await _context.Webhooks
            .FirstOrDefaultAsync(w => w.Id == id && w.TenantId == _tenantContext.TenantId.Value);

        if (webhook == null)
            return false;

        _context.Webhooks.Remove(webhook);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task TriggerWebhookAsync(string eventName, object payload)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            return;
        }

        var webhooks = await _context.Webhooks
            .Where(w => w.TenantId == _tenantContext.TenantId.Value && w.IsActive)
            .ToListAsync();

        var tasks = webhooks
            .Where(w => IsEventSubscribed(w.Events, eventName))
            .Select(w => _webhookDispatcher.DispatchAsync(w, eventName, payload))
            .ToList();

        await Task.WhenAll(tasks);
    }

    private static bool IsEventSubscribed(string eventsJson, string eventName)
    {
        try
        {
            var events = DeserializeEvents(eventsJson);
            return events.Contains(eventName);
        }
        catch
        {
            return false;
        }
    }

    private static List<string> DeserializeEvents(string eventsJson)
    {
        if (string.IsNullOrWhiteSpace(eventsJson))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(eventsJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}

// DTOs
public class WebhookDto
{
    public int Id { get; set; }
    public string Url { get; set; } = string.Empty;
    public List<string> Events { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class CreateWebhookRequest
{
    public string Url { get; set; } = string.Empty;
    public string? Secret { get; set; }
    public List<string> Events { get; set; } = new();
    public bool IsActive { get; set; } = true;
}

public class UpdateWebhookRequest
{
    public string? Url { get; set; }
    public string? Secret { get; set; }
    public List<string>? Events { get; set; }
    public bool? IsActive { get; set; }
}
