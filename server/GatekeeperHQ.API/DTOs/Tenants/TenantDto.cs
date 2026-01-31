namespace GatekeeperHQ.API.DTOs.Tenants;

public class TenantDto
{
	public int Id { get; set; }
	public string Name { get; set; } = string.Empty;
	public string? ApiKey { get; set; }
	public bool IsActive { get; set; }
	public DateTime CreatedAt { get; set; }
	public DateTime UpdatedAt { get; set; }
}

public class CreateTenantRequest
{
	public string Name { get; set; } = string.Empty;
	public bool IsActive { get; set; } = true;
}

public class UpdateTenantRequest
{
	public string? Name { get; set; }
	public bool? IsActive { get; set; }
}

public class RegenerateApiKeyResponse
{
	public string ApiKey { get; set; } = string.Empty;
}
