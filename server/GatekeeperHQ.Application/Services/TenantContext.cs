namespace GatekeeperHQ.Application.Services;

public class TenantContext : ITenantContext
{
	private int? _tenantId;

	public int? TenantId => _tenantId;

	public void SetTenant(int tenantId)
	{
		_tenantId = tenantId;
	}

	public void ClearTenant()
	{
		_tenantId = null;
	}
}
