using GatekeeperHQ.Application.Services;
using GatekeeperHQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace GatekeeperHQ.API.Middleware;

public class ApiKeyAuthenticationMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ApiKeyAuthenticationMiddleware> _logger;

    public ApiKeyAuthenticationMiddleware(RequestDelegate next, ILogger<ApiKeyAuthenticationMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(
        HttpContext context,
        IApiKeyService apiKeyService,
        ITenantContext tenantContext)
    {
        // Skip if already authenticated
        if (context.User.Identity?.IsAuthenticated == true)
        {
            await _next(context);
            return;
        }

        // Try to get API key from header
        if (context.Request.Headers.TryGetValue("X-API-Key", out var apiKeyHeader))
        {
            var apiKey = apiKeyHeader.ToString();
            if (!string.IsNullOrEmpty(apiKey))
            {
                var keyEntity = await apiKeyService.ValidateApiKeyAsync(apiKey);
                if (keyEntity != null)
                {
                    // Set tenant context
                    tenantContext.SetTenant(keyEntity.TenantId);

                    // Create claims identity for API key
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.NameIdentifier, $"api_key_{keyEntity.Id}"),
                        new Claim("api_key_id", keyEntity.Id.ToString()),
                        new Claim("tenant_id", keyEntity.TenantId.ToString())
                    };

                    // Add permissions from API key
                    try
                    {
                        var permissions = System.Text.Json.JsonSerializer.Deserialize<List<string>>(keyEntity.Permissions) ?? new List<string>();
                        foreach (var permission in permissions)
                        {
                            claims.Add(new Claim("permission", permission));
                        }
                    }
                    catch
                    {
                        // Invalid permissions JSON, continue without permissions
                    }

                    var identity = new ClaimsIdentity(claims, "ApiKey");
                    context.User = new ClaimsPrincipal(identity);

                    // Record usage
                    _ = Task.Run(async () => await apiKeyService.RecordApiKeyUsageAsync(keyEntity.Id));

                    _logger.LogDebug("API key authenticated: {ApiKeyId}", keyEntity.Id);
                }
            }
        }

        await _next(context);
    }
}
