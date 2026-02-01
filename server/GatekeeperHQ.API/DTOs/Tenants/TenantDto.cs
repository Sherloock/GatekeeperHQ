using System.ComponentModel.DataAnnotations;

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
	[Required(ErrorMessage = "Name is required")]
	[MinLength(1, ErrorMessage = "Name cannot be empty")]
	[MaxLength(200, ErrorMessage = "Name must be less than 200 characters")]
	[RegularExpression(@"^\S.*\S$|^\S$", ErrorMessage = "Name cannot be only whitespace")]
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
