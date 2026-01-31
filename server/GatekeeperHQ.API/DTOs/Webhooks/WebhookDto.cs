namespace GatekeeperHQ.API.DTOs.Webhooks;

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
