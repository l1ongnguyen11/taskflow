using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Labels;
using TaskFlow.Application.DTOs.Tasks;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.API.Controllers;

[ApiController]
[Authorize]
public class LabelsController : ControllerBase
{
    private readonly ILabelService _labelService;
    private readonly IValidator<CreateLabelRequest> _createLabelValidator;
    private readonly IValidator<UpdateLabelRequest> _updateLabelValidator;
    private readonly IValidator<AddLabelToTaskRequest> _addLabelToTaskValidator;

    public LabelsController(
        ILabelService labelService,
        IValidator<CreateLabelRequest> createLabelValidator,
        IValidator<UpdateLabelRequest> updateLabelValidator,
        IValidator<AddLabelToTaskRequest> addLabelToTaskValidator)
    {
        _labelService = labelService;
        _createLabelValidator = createLabelValidator;
        _updateLabelValidator = updateLabelValidator;
        _addLabelToTaskValidator = addLabelToTaskValidator;
    }

    [HttpPost("api/workspaces/{workspaceId:guid}/labels")]
    [ProducesResponseType(typeof(ApiResponse<LabelResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<LabelResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<LabelResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<LabelResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<LabelResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateLabel(Guid workspaceId, [FromBody] CreateLabelRequest request)
    {
        var validationResult = await _createLabelValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<LabelResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _labelService.CreateLabelAsync(userId, workspaceId, request);
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
            return Unauthorized(ApiResponse<LabelResponse>.FailResponse(ex.Message));
        }
    }

    [HttpGet("api/workspaces/{workspaceId:guid}/labels")]
    [ProducesResponseType(typeof(ApiResponse<List<LabelResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<LabelResponse>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<LabelResponse>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<List<LabelResponse>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLabelsByWorkspaceId(Guid workspaceId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _labelService.GetLabelsByWorkspaceIdAsync(userId, workspaceId);
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
            return Unauthorized(ApiResponse<List<LabelResponse>>.FailResponse(ex.Message));
        }
    }

    [HttpGet("api/labels/{labelId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LabelResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LabelResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<LabelResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<LabelResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetLabelById(Guid labelId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _labelService.GetLabelByIdAsync(userId, labelId);
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
            return Unauthorized(ApiResponse<LabelResponse>.FailResponse(ex.Message));
        }
    }

    [HttpPatch("api/labels/{labelId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<LabelResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<LabelResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<LabelResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<LabelResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<LabelResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateLabel(Guid labelId, [FromBody] UpdateLabelRequest request)
    {
        var validationResult = await _updateLabelValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<LabelResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _labelService.UpdateLabelAsync(userId, labelId, request);
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
            return Unauthorized(ApiResponse<LabelResponse>.FailResponse(ex.Message));
        }
    }

    [HttpDelete("api/labels/{labelId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteLabel(Guid labelId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _labelService.DeleteLabelAsync(userId, labelId);
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
            return Unauthorized(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    [HttpPost("api/tasks/{taskId:guid}/labels")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddLabelToTask(Guid taskId, [FromBody] AddLabelToTaskRequest request)
    {
        var validationResult = await _addLabelToTaskValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _labelService.AddLabelToTaskAsync(userId, taskId, request.LabelId);
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
            return Unauthorized(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    [HttpDelete("api/tasks/{taskId:guid}/labels/{labelId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveLabelFromTask(Guid taskId, Guid labelId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _labelService.RemoveLabelFromTaskAsync(userId, taskId, labelId);
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
            return Unauthorized(ApiResponse<object>.FailResponse(ex.Message));
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
