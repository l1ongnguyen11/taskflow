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
using TaskFlow.Application.DTOs.Activities;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.API.Controllers;

[ApiController]
[Authorize]
public class ActivitiesController : ControllerBase
{
    private readonly IActivityService _activityService;
    private readonly IValidator<LogActivityRequest> _logActivityValidator;

    public ActivitiesController(
        IActivityService activityService,
        IValidator<LogActivityRequest> logActivityValidator)
    {
        _activityService = activityService;
        _logActivityValidator = logActivityValidator;
    }

    [HttpPost("api/workspaces/{workspaceId:guid}/activities")]
    [ProducesResponseType(typeof(ApiResponse<ActivityResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ActivityResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ActivityResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ActivityResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ActivityResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> LogActivity(Guid workspaceId, [FromBody] LogActivityRequest request)
    {
        var validationResult = await _logActivityValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<ActivityResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _activityService.LogActivityAsync(userId, workspaceId, request);
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<ActivityResponse>.FailResponse(ex.Message));
        }
    }

    [HttpGet("api/workspaces/{workspaceId:guid}/activities")]
    [ProducesResponseType(typeof(PaginatedResponse<ActivityResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginatedResponse<ActivityResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(PaginatedResponse<ActivityResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(PaginatedResponse<ActivityResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetActivityTimeline(
        Guid workspaceId,
        [FromQuery] string? entityType,
        [FromQuery] Guid? actorId,
        [FromQuery] Guid? entityId,
        [FromQuery] string? action,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0)
    {
        if (limit <= 0) limit = 20;
        if (offset < 0) offset = 0;

        try
        {
            var userId = GetUserId();
            var result = await _activityService.GetActivityTimelineAsync(
                userId, workspaceId, entityType, actorId, entityId, action, limit, offset);
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(PaginatedResponse<ActivityResponse>.FailResponse(ex.Message));
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
