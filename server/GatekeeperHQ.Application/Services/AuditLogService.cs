using GatekeeperHQ.Domain.Entities;
using GatekeeperHQ.Infrastructure.Data;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace GatekeeperHQ.Application.Services;

public interface IAuditLogService
{
    Task LogAsync(string action, string entityType, int? entityId, object? changes = null);
}

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(
        AppDbContext context,
        ITenantContext tenantContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _tenantContext = tenantContext;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string entityType, int? entityId, object? changes = null)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            return; // Can't log without tenant context
        }

        var httpContext = _httpContextAccessor.HttpContext;
        var userId = httpContext?.User?.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
            ?? httpContext?.User?.FindFirst("sub")?.Value;

        var log = new AuditLog
        {
            UserId = userId != null && int.TryParse(userId, out var uid) ? uid : null,
            TenantId = _tenantContext.TenantId.Value,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Changes = changes != null ? JsonSerializer.Serialize(changes) : null,
            IpAddress = httpContext?.Connection?.RemoteIpAddress?.ToString(),
            UserAgent = httpContext?.Request?.Headers["User-Agent"].ToString(),
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
