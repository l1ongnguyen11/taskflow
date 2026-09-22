using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Workspaces;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.API.Controllers;

[ApiController]
[Route("api/workspaces")]
[Authorize]
public class WorkspaceMembersController : ControllerBase
{
    private readonly IWorkspaceMemberService _workspaceMemberService;
    private readonly IValidator<InviteMemberRequest> _inviteMemberValidator;
    private readonly IValidator<ChangeRoleRequest> _changeRoleValidator;

    public WorkspaceMembersController(
        IWorkspaceMemberService workspaceMemberService,
        IValidator<InviteMemberRequest> inviteMemberValidator,
        IValidator<ChangeRoleRequest> changeRoleValidator)
    {
        _workspaceMemberService = workspaceMemberService;
        _inviteMemberValidator = inviteMemberValidator;
        _changeRoleValidator = changeRoleValidator;
    }

    [HttpPost("{workspaceId:guid}/invitations")]
    [ProducesResponseType(typeof(ApiResponse<InviteMemberResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<InviteMemberResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<InviteMemberResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<InviteMemberResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<InviteMemberResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> InviteMember(Guid workspaceId, [FromBody] InviteMemberRequest request)
    {
        var validationResult = await _inviteMemberValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            return BadRequest(ApiResponse<InviteMemberResponse>.FailResponse("Validation failed.", errors));
        }

        try
        {
            var userId = GetUserId();
            var result = await _workspaceMemberService.InviteMemberAsync(userId, workspaceId, request);
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }
                if (result.Message.Contains("permission") || result.Message.Contains("Only workspace"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, result);
                }
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<InviteMemberResponse>.FailResponse(ex.Message));
        }
    }

    [HttpPost("/api/invitations/{token}/accept")]
    [ProducesResponseType(typeof(ApiResponse<AcceptInvitationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<AcceptInvitationResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> AcceptInvitation(string token)
    {
        try
        {
            var userId = GetUserId();
            var result = await _workspaceMemberService.AcceptInvitationAsync(userId, token);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<AcceptInvitationResponse>.FailResponse(ex.Message));
        }
    }

    [HttpPost("/api/invitations/{token}/reject")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RejectInvitation(string token)
    {
        try
        {
            var userId = GetUserId();
            var result = await _workspaceMemberService.RejectInvitationAsync(userId, token);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    [HttpGet("/api/invitations/me")]
    [ProducesResponseType(typeof(ApiResponse<List<InvitationResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<InvitationResponse>>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyInvitations()
    {
        try
        {
            var userId = GetUserId();
            var result = await _workspaceMemberService.GetMyInvitationsAsync(userId);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<List<InvitationResponse>>.FailResponse(ex.Message));
        }
    }

    [HttpDelete("{workspaceId:guid}/members/{userId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(Guid workspaceId, Guid userId)
    {
        try
        {
            var currentUserId = GetUserId();
            var result = await _workspaceMemberService.RemoveMemberAsync(currentUserId, workspaceId, userId);
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }
                if (result.Message.Contains("permission") || result.Message.Contains("cannot be removed"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, result);
                }
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    [HttpDelete("{workspaceId:guid}/invitations/{invitationId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelInvitation(Guid workspaceId, Guid invitationId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _workspaceMemberService.CancelInvitationAsync(userId, workspaceId, invitationId);
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }
                if (result.Message.Contains("permission") || result.Message.Contains("Only workspace"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, result);
                }
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    [HttpGet("{workspaceId:guid}/invitations")]
    [ProducesResponseType(typeof(PaginatedResponse<InvitationResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginatedResponse<InvitationResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(PaginatedResponse<InvitationResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetWorkspaceInvitations(Guid workspaceId, [FromQuery] string? status = null, [FromQuery] int limit = 20, [FromQuery] int offset = 0)
    {
        if (limit <= 0) limit = 20;
        if (offset < 0) offset = 0;

        try
        {
            var userId = GetUserId();
            var result = await _workspaceMemberService.GetWorkspaceInvitationsAsync(userId, workspaceId, status, limit, offset);
            if (!result.Success)
            {
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(PaginatedResponse<InvitationResponse>.FailResponse(ex.Message));
        }
    }

    [HttpPatch("{workspaceId:guid}/members/{userId:guid}/role")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ChangeRole(Guid workspaceId, Guid userId, [FromBody] ChangeRoleRequest request)
    {
        var validationResult = await _changeRoleValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.FailResponse("Validation failed.", errors));
        }

        try
        {
            var currentUserId = GetUserId();
            var result = await _workspaceMemberService.ChangeMemberRoleAsync(currentUserId, workspaceId, userId, request);
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }
                if (result.Message.Contains("permission") || result.Message.Contains("owners") || result.Message.Contains("owner"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, result);
                }
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    [HttpGet("{workspaceId:guid}/members")]
    [ProducesResponseType(typeof(PaginatedResponse<WorkspaceMemberResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginatedResponse<WorkspaceMemberResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(PaginatedResponse<WorkspaceMemberResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetMembers(Guid workspaceId, [FromQuery] string? search = null, [FromQuery] int limit = 20, [FromQuery] int offset = 0)
    {
        if (limit <= 0) limit = 20;
        if (offset < 0) offset = 0;

        try
        {
            var currentUserId = GetUserId();
            var result = await _workspaceMemberService.GetMembersAsync(currentUserId, workspaceId, search, limit, offset);
            if (!result.Success)
            {
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(PaginatedResponse<WorkspaceMemberResponse>.FailResponse(ex.Message));
        }
    }

    private Guid GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user claims.");
        }

        return userId;
    }
}
