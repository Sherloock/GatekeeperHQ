using GatekeeperHQ.Application.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace GatekeeperHQ.API.Filters;

/// <summary>
/// Action filter that validates tenant context is set before executing the action.
/// Returns 400 Bad Request if tenant context is not available.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public class RequireTenantContextAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(ActionExecutingContext context)
    {
        var tenantContext = context.HttpContext.RequestServices.GetService<ITenantContext>();

        if (tenantContext == null || !tenantContext.TenantId.HasValue)
        {
            context.Result = new BadRequestObjectResult(new
            {
                message = "Tenant context is required. Please include X-Tenant-Id header or ensure your token includes a tenant claim."
            });
            return;
        }

        base.OnActionExecuting(context);
    }
}
