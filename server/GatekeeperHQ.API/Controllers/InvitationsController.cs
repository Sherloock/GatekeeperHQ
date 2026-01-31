using System.Security.Claims;
using GatekeeperHQ.Application.Services;
using GatekeeperHQ.Domain.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ApiAcceptInvitationRequest = GatekeeperHQ.API.DTOs.Invitations.AcceptInvitationRequest;
using ApiAcceptInvitationResponse = GatekeeperHQ.API.DTOs.Invitations.AcceptInvitationResponse;
using ApiCreateInvitationRequest = GatekeeperHQ.API.DTOs.Invitations.CreateInvitationRequest;
using ApiInvitationDto = GatekeeperHQ.API.DTOs.Invitations.InvitationDto;
using ApiValidateInvitationResponse = GatekeeperHQ.API.DTOs.Invitations.ValidateInvitationResponse;

namespace GatekeeperHQ.API.Controllers;

[ApiController]
[Route("api/v{version:apiVersion}/[controller]")]
[ApiVersion("1.0")]
public class InvitationsController : ControllerBase
{
	private readonly IInvitationService _invitationService;

	public InvitationsController(IInvitationService invitationService)
	{
		_invitationService = invitationService;
	}

	/// <summary>
	/// Get all invitations for a tenant (Super Admin only)
	/// </summary>
	[HttpGet("tenants/{tenantId}")]
	[Authorize(Policy = Permissions.InvitationsManage)]
	public async Task<ActionResult<List<ApiInvitationDto>>> GetTenantInvitations(int tenantId)
	{
		var invitations = await _invitationService.GetTenantInvitationsAsync(tenantId);
		var result = invitations.Select(MapToDto).ToList();
		return Ok(result);
	}

	/// <summary>
	/// Create a new invitation (Super Admin only)
	/// </summary>
	[HttpPost("tenants/{tenantId}")]
	[Authorize(Policy = Permissions.InvitationsManage)]
	public async Task<ActionResult<ApiInvitationDto>> CreateInvitation(int tenantId, [FromBody] ApiCreateInvitationRequest request)
	{
		var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
			?? User.FindFirst("sub")?.Value;

		if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out var userId))
		{
			return Unauthorized(new { message = "Invalid token" });
		}

		try
		{
			var invitation = await _invitationService.CreateInvitationAsync(
				tenantId,
				new Application.Services.CreateInvitationRequest
				{
					Email = request.Email,
					RoleId = request.RoleId
				},
				userId);

			return CreatedAtAction(nameof(ValidateInvitation), new { token = invitation.Token }, MapToDto(invitation));
		}
		catch (InvalidOperationException ex)
		{
			return Conflict(new { message = ex.Message });
		}
	}

	/// <summary>
	/// Revoke an invitation (Super Admin only)
	/// </summary>
	[HttpDelete("{id}")]
	[Authorize(Policy = Permissions.InvitationsManage)]
	public async Task<IActionResult> RevokeInvitation(int id)
	{
		try
		{
			var result = await _invitationService.RevokeInvitationAsync(id);
			if (!result)
				return NotFound(new { message = "Invitation not found" });

			return NoContent();
		}
		catch (InvalidOperationException ex)
		{
			return Conflict(new { message = ex.Message });
		}
	}

	/// <summary>
	/// Validate an invitation token (Public)
	/// </summary>
	[HttpGet("{token}/validate")]
	[AllowAnonymous]
	public async Task<ActionResult<ApiValidateInvitationResponse>> ValidateInvitation(string token)
	{
		var invitation = await _invitationService.GetInvitationByTokenAsync(token);

		if (invitation == null)
		{
			return Ok(new ApiValidateInvitationResponse
			{
				Valid = false,
				Error = "Invalid invitation token"
			});
		}

		if (invitation.Status == "expired")
		{
			return Ok(new ApiValidateInvitationResponse
			{
				Valid = false,
				Error = "Invitation has expired"
			});
		}

		if (invitation.Status == "accepted")
		{
			return Ok(new ApiValidateInvitationResponse
			{
				Valid = false,
				Error = "Invitation has already been accepted"
			});
		}

		return Ok(new ApiValidateInvitationResponse
		{
			Valid = true,
			Email = invitation.Email,
			TenantName = invitation.TenantName,
			RoleName = invitation.RoleName,
			ExpiresAt = invitation.ExpiresAt
		});
	}

	/// <summary>
	/// Accept an invitation and create user account (Public)
	/// </summary>
	[HttpPost("{token}/accept")]
	[AllowAnonymous]
	public async Task<ActionResult<ApiAcceptInvitationResponse>> AcceptInvitation(string token, [FromBody] ApiAcceptInvitationRequest request)
	{
		if (string.IsNullOrWhiteSpace(request.Password) || request.Password.Length < 6)
		{
			return BadRequest(new { message = "Password must be at least 6 characters" });
		}

		var result = await _invitationService.AcceptInvitationAsync(token, request.Password);

		if (!result.Success)
		{
			return BadRequest(new ApiAcceptInvitationResponse
			{
				Success = false,
				Error = result.Error
			});
		}

		return Ok(new ApiAcceptInvitationResponse
		{
			Success = true,
			UserId = result.UserId,
			Email = result.Email,
			TenantId = result.TenantId,
			TenantName = result.TenantName
		});
	}

	private static ApiInvitationDto MapToDto(InvitationDto invitation)
	{
		return new ApiInvitationDto
		{
			Id = invitation.Id,
			TenantId = invitation.TenantId,
			TenantName = invitation.TenantName,
			Email = invitation.Email,
			Token = invitation.Token,
			RoleId = invitation.RoleId,
			RoleName = invitation.RoleName,
			ExpiresAt = invitation.ExpiresAt,
			AcceptedAt = invitation.AcceptedAt,
			CreatedByUserId = invitation.CreatedByUserId,
			CreatedByEmail = invitation.CreatedByEmail,
			CreatedAt = invitation.CreatedAt,
			Status = invitation.Status
		};
	}
}
