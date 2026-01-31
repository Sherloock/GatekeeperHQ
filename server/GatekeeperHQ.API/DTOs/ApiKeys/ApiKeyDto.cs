namespace GatekeeperHQ.API.DTOs.ApiKeys;

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
