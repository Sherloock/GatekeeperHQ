using GatekeeperHQ.API.DTOs.ApiKeys;
using GatekeeperHQ.API.Filters;
using GatekeeperHQ.Application.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GatekeeperHQ.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
[RequireTenantContext]
public class ApiKeysController : ControllerBase
{
    private readonly IApiKeyService _apiKeyService;

    public ApiKeysController(IApiKeyService apiKeyService)
    {
        _apiKeyService = apiKeyService;
    }

    [HttpGet]
    public async Task<ActionResult<List<DTOs.ApiKeys.ApiKeyDto>>> GetApiKeys()
    {
        try
        {
            var apiKeys = await _apiKeyService.GetAllApiKeysAsync();
            var result = apiKeys.Select(k => new DTOs.ApiKeys.ApiKeyDto
            {
                Id = k.Id,
                Name = k.Name,
                Permissions = k.Permissions,
                IsActive = k.IsActive,
                ExpiresAt = k.ExpiresAt,
                CreatedAt = k.CreatedAt,
                UpdatedAt = k.UpdatedAt,
                LastUsedAt = k.LastUsedAt
            }).ToList();
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DTOs.ApiKeys.ApiKeyDto>> GetApiKey(int id)
    {
        try
        {
            var apiKey = await _apiKeyService.GetApiKeyByIdAsync(id);
            if (apiKey == null)
                return NotFound(new { message = "API key not found" });

            var result = new DTOs.ApiKeys.ApiKeyDto
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                Permissions = apiKey.Permissions,
                IsActive = apiKey.IsActive,
                ExpiresAt = apiKey.ExpiresAt,
                CreatedAt = apiKey.CreatedAt,
                UpdatedAt = apiKey.UpdatedAt,
                LastUsedAt = apiKey.LastUsedAt
            };
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<DTOs.ApiKeys.CreateApiKeyResponse>> CreateApiKey([FromBody] DTOs.ApiKeys.CreateApiKeyRequest request)
    {
        try
        {
            var apiKey = await _apiKeyService.CreateApiKeyAsync(new Application.Services.CreateApiKeyRequest
            {
                Name = request.Name,
                Permissions = request.Permissions,
                IsActive = request.IsActive,
                ExpiresAt = request.ExpiresAt
            });

            var result = new DTOs.ApiKeys.CreateApiKeyResponse
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                Key = apiKey.Key,
                Permissions = apiKey.Permissions,
                IsActive = apiKey.IsActive,
                ExpiresAt = apiKey.ExpiresAt,
                CreatedAt = apiKey.CreatedAt
            };
            return CreatedAtAction(nameof(GetApiKey), new { id = apiKey.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<DTOs.ApiKeys.ApiKeyDto>> UpdateApiKey(int id, [FromBody] DTOs.ApiKeys.UpdateApiKeyRequest request)
    {
        try
        {
            var apiKey = await _apiKeyService.UpdateApiKeyAsync(id, new Application.Services.UpdateApiKeyRequest
            {
                Name = request.Name,
                Permissions = request.Permissions,
                IsActive = request.IsActive,
                ExpiresAt = request.ExpiresAt
            });

            if (apiKey == null)
                return NotFound(new { message = "API key not found" });

            var result = new DTOs.ApiKeys.ApiKeyDto
            {
                Id = apiKey.Id,
                Name = apiKey.Name,
                Permissions = apiKey.Permissions,
                IsActive = apiKey.IsActive,
                ExpiresAt = apiKey.ExpiresAt,
                CreatedAt = apiKey.CreatedAt,
                UpdatedAt = apiKey.UpdatedAt,
                LastUsedAt = apiKey.LastUsedAt
            };
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteApiKey(int id)
    {
        try
        {
            var result = await _apiKeyService.DeleteApiKeyAsync(id);
            if (!result)
                return NotFound(new { message = "API key not found" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
