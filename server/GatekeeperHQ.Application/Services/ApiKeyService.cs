using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using GatekeeperHQ.Domain.Entities;
using GatekeeperHQ.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace GatekeeperHQ.Application.Services;

public interface IApiKeyService
{
    Task<List<ApiKeyDto>> GetAllApiKeysAsync();
    Task<ApiKeyDto?> GetApiKeyByIdAsync(int id);
    Task<CreateApiKeyResponse> CreateApiKeyAsync(CreateApiKeyRequest request);
    Task<ApiKeyDto?> UpdateApiKeyAsync(int id, UpdateApiKeyRequest request);
    Task<bool> DeleteApiKeyAsync(int id);
    Task<ApiKey?> ValidateApiKeyAsync(string apiKey);
    Task RecordApiKeyUsageAsync(int apiKeyId);
}

public class ApiKeyService : IApiKeyService
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;

    public ApiKeyService(AppDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    public async Task<List<ApiKeyDto>> GetAllApiKeysAsync()
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        var apiKeys = await _context.ApiKeys
            .Where(k => k.TenantId == _tenantContext.TenantId.Value)
            .OrderBy(k => k.Name)
            .ToListAsync();

        return apiKeys.Select(k => new ApiKeyDto
        {
            Id = k.Id,
            Name = k.Name,
            Permissions = DeserializePermissions(k.Permissions),
            IsActive = k.IsActive,
            ExpiresAt = k.ExpiresAt,
            CreatedAt = k.CreatedAt,
            UpdatedAt = k.UpdatedAt,
            LastUsedAt = k.LastUsedAt
        }).ToList();
    }

    public async Task<ApiKeyDto?> GetApiKeyByIdAsync(int id)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == id && k.TenantId == _tenantContext.TenantId.Value);

        if (apiKey == null)
            return null;

        return new ApiKeyDto
        {
            Id = apiKey.Id,
            Name = apiKey.Name,
            Permissions = DeserializePermissions(apiKey.Permissions),
            IsActive = apiKey.IsActive,
            ExpiresAt = apiKey.ExpiresAt,
            CreatedAt = apiKey.CreatedAt,
            UpdatedAt = apiKey.UpdatedAt,
            LastUsedAt = apiKey.LastUsedAt
        };
    }

    public async Task<CreateApiKeyResponse> CreateApiKeyAsync(CreateApiKeyRequest request)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        // Generate API key
        var keyValue = GenerateApiKey();
        var keyHash = HashApiKey(keyValue);

        var apiKey = new ApiKey
        {
            TenantId = _tenantContext.TenantId.Value,
            Name = request.Name,
            Key = keyValue, // Store plain text only for initial response
            KeyHash = keyHash,
            Permissions = JsonSerializer.Serialize(request.Permissions ?? new List<string>()),
            IsActive = request.IsActive,
            ExpiresAt = request.ExpiresAt,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _context.ApiKeys.Add(apiKey);
        await _context.SaveChangesAsync();

        // Clear the key from entity (only return in response)
        var responseKey = apiKey.Key;
        apiKey.Key = string.Empty; // Don't store plain text

        return new CreateApiKeyResponse
        {
            Id = apiKey.Id,
            Name = apiKey.Name,
            Key = responseKey, // Only time the key is returned
            Permissions = request.Permissions ?? new List<string>(),
            IsActive = apiKey.IsActive,
            ExpiresAt = apiKey.ExpiresAt,
            CreatedAt = apiKey.CreatedAt
        };
    }

    public async Task<ApiKeyDto?> UpdateApiKeyAsync(int id, UpdateApiKeyRequest request)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == id && k.TenantId == _tenantContext.TenantId.Value);

        if (apiKey == null)
            return null;

        if (!string.IsNullOrEmpty(request.Name))
        {
            apiKey.Name = request.Name;
        }

        if (request.Permissions != null)
        {
            apiKey.Permissions = JsonSerializer.Serialize(request.Permissions);
        }

        if (request.IsActive.HasValue)
        {
            apiKey.IsActive = request.IsActive.Value;
        }

        if (request.ExpiresAt.HasValue)
        {
            apiKey.ExpiresAt = request.ExpiresAt;
        }

        apiKey.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return await GetApiKeyByIdAsync(id);
    }

    public async Task<bool> DeleteApiKeyAsync(int id)
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            throw new InvalidOperationException("Tenant context is required");
        }

        var apiKey = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.Id == id && k.TenantId == _tenantContext.TenantId.Value);

        if (apiKey == null)
            return false;

        _context.ApiKeys.Remove(apiKey);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<ApiKey?> ValidateApiKeyAsync(string apiKey)
    {
        var keyHash = HashApiKey(apiKey);
        var keyEntity = await _context.ApiKeys
            .FirstOrDefaultAsync(k => k.KeyHash == keyHash
                && k.IsActive
                && (k.ExpiresAt == null || k.ExpiresAt > DateTime.UtcNow));

        return keyEntity;
    }

    public async Task RecordApiKeyUsageAsync(int apiKeyId)
    {
        var apiKey = await _context.ApiKeys.FindAsync(apiKeyId);
        if (apiKey != null)
        {
            apiKey.LastUsedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
        }
    }

    private static string GenerateApiKey()
    {
        // Generate a secure API key: gk_ prefix + 32 random bytes base64 encoded
        var bytes = new byte[32];
        RandomNumberGenerator.Fill(bytes);
        var base64 = Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
        return $"gk_{base64}";
    }

    private static string HashApiKey(string apiKey)
    {
        var bytes = Encoding.UTF8.GetBytes(apiKey);
        var hash = SHA256.HashData(bytes);
        return Convert.ToHexString(hash).ToLower();
    }

    private static List<string> DeserializePermissions(string permissionsJson)
    {
        if (string.IsNullOrWhiteSpace(permissionsJson))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(permissionsJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }
}

// DTOs
public class ApiKeyDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<string> Permissions { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}

public class CreateApiKeyRequest
{
    public string Name { get; set; } = string.Empty;
    public List<string>? Permissions { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime? ExpiresAt { get; set; }
}

public class CreateApiKeyResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty; // Only returned on creation
    public List<string> Permissions { get; set; } = new();
    public bool IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class UpdateApiKeyRequest
{
    public string? Name { get; set; }
    public List<string>? Permissions { get; set; }
    public bool? IsActive { get; set; }
    public DateTime? ExpiresAt { get; set; }
}
