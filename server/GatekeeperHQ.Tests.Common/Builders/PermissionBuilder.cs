using GatekeeperHQ.Domain.Entities;

namespace GatekeeperHQ.Tests.Common.Builders;

public class PermissionBuilder
{
	private int? _id;
	private int _tenantId;
	private Tenant? _tenant;
	private string? _key;
	private string? _description;

	public PermissionBuilder WithId(int id)
	{
		_id = id;
		return this;
	}

	public PermissionBuilder WithTenantId(int tenantId)
	{
		_tenantId = tenantId;
		return this;
	}

	public PermissionBuilder WithTenant(Tenant tenant)
	{
		_tenant = tenant;
		_tenantId = tenant.Id;
		return this;
	}

	public PermissionBuilder WithKey(string key)
	{
		_key = key;
		return this;
	}

	public PermissionBuilder WithDescription(string description)
	{
		_description = description;
		return this;
	}

	public Permission Build()
	{
		return new Permission
		{
			Id = _id ?? 0,
			TenantId = _tenantId,
			Tenant = _tenant!,
			Key = _key ?? "resource.action",
			Description = _description
		};
	}

	public static PermissionBuilder Default() => new();
	public static PermissionBuilder UsersView() => new PermissionBuilder().WithKey("users.view");
	public static PermissionBuilder UsersCreate() => new PermissionBuilder().WithKey("users.create");
	public static PermissionBuilder UsersEdit() => new PermissionBuilder().WithKey("users.edit");
	public static PermissionBuilder UsersDelete() => new PermissionBuilder().WithKey("users.delete");
	public static PermissionBuilder RolesView() => new PermissionBuilder().WithKey("roles.view");
	public static PermissionBuilder RolesManage() => new PermissionBuilder().WithKey("roles.manage");
}
