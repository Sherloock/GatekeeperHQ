using Microsoft.AspNetCore.Authorization;

namespace GatekeeperHQ.Infrastructure.Auth;

public class PermissionRequirement : IAuthorizationRequirement
{
	public string Permission { get; }

	public PermissionRequirement(string permission)
	{
		Permission = permission;
	}
}

public class PermissionAuthorizationHandler : AuthorizationHandler<PermissionRequirement>
{
	protected override Task HandleRequirementAsync(
		AuthorizationHandlerContext context,
		PermissionRequirement requirement)
	{
		// Super Admin bypass - grant all permissions
		var isSuperAdminClaim = context.User.Claims
			.FirstOrDefault(c => c.Type == "is_super_admin")?.Value;

		if (isSuperAdminClaim == "true")
		{
			context.Succeed(requirement);
			return Task.CompletedTask;
		}

		var permissions = context.User.Claims
			.Where(c => c.Type == "permission")
			.Select(c => c.Value)
			.ToList();

		if (permissions.Contains(requirement.Permission))
		{
			context.Succeed(requirement);
		}

		return Task.CompletedTask;
	}
}
