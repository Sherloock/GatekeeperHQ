using GatekeeperHQ.API.DTOs.Users;
using GatekeeperHQ.Application.Services;
using GatekeeperHQ.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace GatekeeperHQ.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [Authorize(Policy = Permissions.UsersView)]
    public async Task<ActionResult<List<UserDto>>> GetUsers()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("{id}")]
    [Authorize(Policy = Permissions.UsersView)]
    public async Task<ActionResult<UserDto>> GetUser(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null)
            return NotFound(new { message = "User not found" });

        return Ok(user);
    }

    [HttpPost]
    [Authorize(Policy = Permissions.UsersCreate)]
    public async Task<ActionResult<UserDto>> CreateUser([FromBody] CreateUserRequest request)
    {
        try
        {
            var user = await _userService.CreateUserAsync(new Application.Services.CreateUserRequest
            {
                Email = request.Email,
                Password = request.Password,
                IsActive = request.IsActive,
                RoleIds = request.RoleIds
            });

            return CreatedAtAction(nameof(GetUser), new { id = user.Id }, user);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Permissions.UsersEdit)]
    public async Task<ActionResult<UserDto>> UpdateUser(int id, [FromBody] UpdateUserRequest request)
    {
        try
        {
            var user = await _userService.UpdateUserAsync(id, new Application.Services.UpdateUserRequest
            {
                Email = request.Email,
                Password = request.Password,
                IsActive = request.IsActive,
                RoleIds = request.RoleIds
            });

            if (user == null)
                return NotFound(new { message = "User not found" });

            return Ok(user);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Permissions.UsersDelete)]
    public async Task<IActionResult> DeleteUser(int id)
    {
        var deleted = await _userService.DeleteUserAsync(id);
        if (!deleted)
            return NotFound(new { message = "User not found" });

        return NoContent();
    }
}
