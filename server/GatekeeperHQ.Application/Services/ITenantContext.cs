namespace GatekeeperHQ.Application.Services;

public interface ITenantContext
{
	int? TenantId { get; }
	void SetTenant(int tenantId);
	void ClearTenant();
}
