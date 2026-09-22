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
public class WorkspacesController : ControllerBase
{
    private readonly IWorkspaceService _workspaceService;
    private readonly IValidator<CreateWorkspaceRequest> _createWorkspaceValidator;
    private readonly IValidator<UpdateWorkspaceRequest> _updateWorkspaceValidator;

    public WorkspacesController(
        IWorkspaceService workspaceService,
        IValidator<CreateWorkspaceRequest> createWorkspaceValidator,
        IValidator<UpdateWorkspaceRequest> updateWorkspaceValidator)
    {
        _workspaceService = workspaceService;
        _createWorkspaceValidator = createWorkspaceValidator;
        _updateWorkspaceValidator = updateWorkspaceValidator;
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceResponse>), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateWorkspaceRequest request)
    {
        var validationResult = await _createWorkspaceValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            return BadRequest(ApiResponse<WorkspaceResponse>.FailResponse("Validation failed.", errors));
        }

        try
        {
            var userId = GetUserId();
            var result = await _workspaceService.CreateWorkspaceAsync(userId, request);
            if (!result.Success)
            {
                if (result.Message.Contains("already in use"))
                {
                    return Conflict(result);
                }
                return BadRequest(result);
            }
            return CreatedAtAction(nameof(GetById), new { workspaceId = result.Data!.Id }, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<WorkspaceResponse>.FailResponse(ex.Message));
        }
    }

    [HttpPatch("{workspaceId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(Guid workspaceId, [FromBody] UpdateWorkspaceRequest request)
    {
        var validationResult = await _updateWorkspaceValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(x => x.ErrorMessage).ToList();
            return BadRequest(ApiResponse<WorkspaceResponse>.FailResponse("Validation failed.", errors));
        }

        try
        {
            var userId = GetUserId();
            var result = await _workspaceService.UpdateWorkspaceAsync(userId, workspaceId, request);
            if (!result.Success)
            {
                if (result.Message.Contains("permission") || result.Message.Contains("have access"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, result);
                }
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<WorkspaceResponse>.FailResponse(ex.Message));
        }
    }

    [HttpDelete("{workspaceId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid workspaceId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _workspaceService.DeleteWorkspaceAsync(userId, workspaceId);
            if (!result.Success)
            {
                if (result.Message.Contains("permission") || result.Message.Contains("owner"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, result);
                }
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
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

    [HttpGet("{workspaceId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid workspaceId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _workspaceService.GetWorkspaceAsync(userId, workspaceId);
            if (!result.Success)
            {
                if (result.Message.Contains("access") || result.Message.Contains("permission"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, result);
                }
                return NotFound(result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<WorkspaceResponse>.FailResponse(ex.Message));
        }
    }

    [HttpGet]
    [ProducesResponseType(typeof(PaginatedResponse<WorkspaceResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginatedResponse<WorkspaceResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetMyWorkspaces([FromQuery] int limit = 20, [FromQuery] int offset = 0)
    {
        if (limit <= 0) limit = 20;
        if (offset < 0) offset = 0;

        try
        {
            var userId = GetUserId();
            var result = await _workspaceService.GetMyWorkspacesAsync(userId, limit, offset);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(PaginatedResponse<WorkspaceResponse>.FailResponse(ex.Message));
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
