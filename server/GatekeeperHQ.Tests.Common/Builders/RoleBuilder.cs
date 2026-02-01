using Bogus;
using GatekeeperHQ.Domain.Entities;

namespace GatekeeperHQ.Tests.Common.Builders;

public class RoleBuilder
{
	private readonly Faker _faker = new();
	private int? _id;
	private int _tenantId;
	private Tenant? _tenant;
	private string? _name;
	private string? _description;
	private DateTime? _createdAt;
	private List<RolePermission> _rolePermissions = new();

	public RoleBuilder WithId(int id)
	{
		_id = id;
		return this;
	}

	public RoleBuilder WithTenantId(int tenantId)
	{
		_tenantId = tenantId;
		return this;
	}

	public RoleBuilder WithTenant(Tenant tenant)
	{
		_tenant = tenant;
		_tenantId = tenant.Id;
		return this;
	}

	public RoleBuilder WithName(string name)
	{
		_name = name;
		return this;
	}

	public RoleBuilder WithDescription(string description)
	{
		_description = description;
		return this;
	}

	public RoleBuilder WithPermission(Permission permission)
	{
		_rolePermissions.Add(new RolePermission
		{
			PermissionId = permission.Id,
			Permission = permission
		});
		return this;
	}

	public RoleBuilder WithCreatedAt(DateTime createdAt)
	{
		_createdAt = createdAt;
		return this;
	}

	public Role Build()
	{
		var role = new Role
		{
			Id = _id ?? 0,
			TenantId = _tenantId,
			Tenant = _tenant!,
			Name = _name ?? _faker.Name.JobTitle(),
			Description = _description,
			CreatedAt = _createdAt ?? DateTime.UtcNow
		};

		foreach (var rp in _rolePermissions)
		{
			rp.RoleId = role.Id;
			rp.Role = role;
			role.RolePermissions.Add(rp);
		}

		return role;
	}

	public static RoleBuilder Default() => new();
}
