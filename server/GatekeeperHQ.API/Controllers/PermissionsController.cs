using GatekeeperHQ.API.DTOs.Permissions;
using GatekeeperHQ.Domain.Constants;
using GatekeeperHQ.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GatekeeperHQ.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class PermissionsController : ControllerBase
{
    private readonly AppDbContext _context;

    public PermissionsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.PermissionsView)]
    public async Task<ActionResult<List<PermissionDto>>> GetPermissions()
    {
        var permissions = await _context.Permissions
            .OrderBy(p => p.Key)
            .Select(p => new PermissionDto
            {
                Id = p.Id,
                Key = p.Key,
                Description = p.Description
            })
            .ToListAsync();

        return Ok(permissions);
    }
}
