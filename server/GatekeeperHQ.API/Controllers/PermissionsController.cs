using GatekeeperHQ.API.DTOs.Permissions;
using GatekeeperHQ.Application.Services;
using GatekeeperHQ.Domain.Constants;
using GatekeeperHQ.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GatekeeperHQ.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly ITenantContext _tenantContext;

    public PermissionsController(AppDbContext context, ITenantContext tenantContext)
    {
        _context = context;
        _tenantContext = tenantContext;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.PermissionsView)]
    public async Task<ActionResult<List<DTOs.Permissions.PermissionDto>>> GetPermissions()
    {
        if (!_tenantContext.TenantId.HasValue)
        {
            return BadRequest(new { message = "Tenant context is required" });
        }

        var permissions = await _context.Permissions
            .Where(p => p.TenantId == _tenantContext.TenantId.Value)
            .OrderBy(p => p.Key)
            .Select(p => new DTOs.Permissions.PermissionDto
            {
                Id = p.Id,
                Key = p.Key,
                Description = p.Description
            })
            .ToListAsync();

        return Ok(permissions);
    }
}
