using GatekeeperHQ.API.DTOs.Webhooks;
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
public class WebhooksController : ControllerBase
{
    private readonly IWebhookService _webhookService;

    public WebhooksController(IWebhookService webhookService)
    {
        _webhookService = webhookService;
    }

    [HttpGet]
    public async Task<ActionResult<List<DTOs.Webhooks.WebhookDto>>> GetWebhooks()
    {
        try
        {
            var webhooks = await _webhookService.GetAllWebhooksAsync();
            var result = webhooks.Select(w => new DTOs.Webhooks.WebhookDto
            {
                Id = w.Id,
                Url = w.Url,
                Events = w.Events,
                IsActive = w.IsActive,
                CreatedAt = w.CreatedAt,
                UpdatedAt = w.UpdatedAt
            }).ToList();
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<DTOs.Webhooks.WebhookDto>> GetWebhook(int id)
    {
        try
        {
            var webhook = await _webhookService.GetWebhookByIdAsync(id);
            if (webhook == null)
                return NotFound(new { message = "Webhook not found" });

            var result = new DTOs.Webhooks.WebhookDto
            {
                Id = webhook.Id,
                Url = webhook.Url,
                Events = webhook.Events,
                IsActive = webhook.IsActive,
                CreatedAt = webhook.CreatedAt,
                UpdatedAt = webhook.UpdatedAt
            };
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost]
    public async Task<ActionResult<DTOs.Webhooks.WebhookDto>> CreateWebhook([FromBody] DTOs.Webhooks.CreateWebhookRequest request)
    {
        try
        {
            var webhook = await _webhookService.CreateWebhookAsync(new Application.Services.CreateWebhookRequest
            {
                Url = request.Url,
                Secret = request.Secret,
                Events = request.Events,
                IsActive = request.IsActive
            });

            var result = new DTOs.Webhooks.WebhookDto
            {
                Id = webhook.Id,
                Url = webhook.Url,
                Events = webhook.Events,
                IsActive = webhook.IsActive,
                CreatedAt = webhook.CreatedAt,
                UpdatedAt = webhook.UpdatedAt
            };
            return CreatedAtAction(nameof(GetWebhook), new { id = webhook.Id }, result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    public async Task<ActionResult<DTOs.Webhooks.WebhookDto>> UpdateWebhook(int id, [FromBody] DTOs.Webhooks.UpdateWebhookRequest request)
    {
        try
        {
            var webhook = await _webhookService.UpdateWebhookAsync(id, new Application.Services.UpdateWebhookRequest
            {
                Url = request.Url,
                Secret = request.Secret,
                Events = request.Events,
                IsActive = request.IsActive
            });

            if (webhook == null)
                return NotFound(new { message = "Webhook not found" });

            var result = new DTOs.Webhooks.WebhookDto
            {
                Id = webhook.Id,
                Url = webhook.Url,
                Events = webhook.Events,
                IsActive = webhook.IsActive,
                CreatedAt = webhook.CreatedAt,
                UpdatedAt = webhook.UpdatedAt
            };
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteWebhook(int id)
    {
        try
        {
            var result = await _webhookService.DeleteWebhookAsync(id);
            if (!result)
                return NotFound(new { message = "Webhook not found" });

            return NoContent();
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }
}
