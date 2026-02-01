using GatekeeperHQ.Application.Services;

namespace GatekeeperHQ.Tests.Common.Fixtures;

/// <summary>
/// A mock tenant context for unit testing that allows setting the tenant ID.
/// </summary>
public class MockTenantContext : ITenantContext
{
	public int? TenantId { get; private set; }

	public MockTenantContext()
	{
	}

	public MockTenantContext(int tenantId)
	{
		TenantId = tenantId;
	}

	public void SetTenant(int tenantId)
	{
		TenantId = tenantId;
	}

	public void SetTenantId(int? tenantId)
	{
		TenantId = tenantId;
	}

	public void ClearTenant()
	{
		TenantId = null;
	}
}
