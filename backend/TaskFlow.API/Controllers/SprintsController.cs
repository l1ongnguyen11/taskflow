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
using TaskFlow.Application.DTOs.Sprints;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.API.Controllers;

[ApiController]
[Authorize]
public class SprintsController : ControllerBase
{
    private readonly ISprintService _sprintService;
    private readonly IValidator<CreateSprintRequest> _createSprintValidator;
    private readonly IValidator<UpdateSprintRequest> _updateSprintValidator;
    private readonly IValidator<SprintTaskRequest> _sprintTaskValidator;

    public SprintsController(
        ISprintService sprintService,
        IValidator<CreateSprintRequest> createSprintValidator,
        IValidator<UpdateSprintRequest> updateSprintValidator,
        IValidator<SprintTaskRequest> sprintTaskValidator)
    {
        _sprintService = sprintService;
        _createSprintValidator = createSprintValidator;
        _updateSprintValidator = updateSprintValidator;
        _sprintTaskValidator = sprintTaskValidator;
    }

    [HttpPost("api/projects/{projectId:guid}/sprints")]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateSprint(Guid projectId, [FromBody] CreateSprintRequest request)
    {
        var validationResult = await _createSprintValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<SprintResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _sprintService.CreateSprintAsync(userId, projectId, request);
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }
                if (result.Message.Contains("access") || result.Message.Contains("permission"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, result);
                }
                return BadRequest(result);
            }
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<SprintResponse>.FailResponse(ex.Message));
        }
    }

    [HttpGet("api/projects/{projectId:guid}/sprints")]
    [ProducesResponseType(typeof(PaginatedResponse<SprintResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginatedResponse<SprintResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(PaginatedResponse<SprintResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(PaginatedResponse<SprintResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectSprints(
        Guid projectId,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0)
    {
        if (limit <= 0) limit = 20;
        if (offset < 0) offset = 0;

        try
        {
            var userId = GetUserId();
            var result = await _sprintService.GetProjectSprintsAsync(userId, projectId, limit, offset);
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
            return Unauthorized(PaginatedResponse<SprintResponse>.FailResponse(ex.Message));
        }
    }

    [HttpGet("api/sprints/{sprintId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetSprintById(Guid sprintId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _sprintService.GetSprintByIdAsync(userId, sprintId);
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
            return Unauthorized(ApiResponse<SprintResponse>.FailResponse(ex.Message));
        }
    }

    [HttpPatch("api/sprints/{sprintId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateSprint(Guid sprintId, [FromBody] UpdateSprintRequest request)
    {
        var validationResult = await _updateSprintValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<SprintResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _sprintService.UpdateSprintAsync(userId, sprintId, request);
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }
                if (result.Message.Contains("access") || result.Message.Contains("permission") || result.Message.Contains("already active"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, result);
                }
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<SprintResponse>.FailResponse(ex.Message));
        }
    }

    [HttpDelete("api/sprints/{sprintId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSprint(Guid sprintId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _sprintService.DeleteSprintAsync(userId, sprintId);
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }
                if (result.Message.Contains("access") || result.Message.Contains("permission") || result.Message.Contains("active sprint"))
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

    [HttpPatch("api/sprints/{sprintId:guid}/start")]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartSprint(Guid sprintId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _sprintService.StartSprintAsync(userId, sprintId);
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }
                if (result.Message.Contains("access") || result.Message.Contains("permission") || result.Message.Contains("already active"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, result);
                }
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<SprintResponse>.FailResponse(ex.Message));
        }
    }

    [HttpPatch("api/sprints/{sprintId:guid}/complete")]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CompleteSprint(Guid sprintId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _sprintService.CompleteSprintAsync(userId, sprintId);
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }
                if (result.Message.Contains("access") || result.Message.Contains("permission"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, result);
                }
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<SprintResponse>.FailResponse(ex.Message));
        }
    }

    [HttpPost("api/sprints/{sprintId:guid}/tasks")]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddTaskToSprint(Guid sprintId, [FromBody] SprintTaskRequest request)
    {
        var validationResult = await _sprintTaskValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<SprintResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _sprintService.AddTaskToSprintAsync(userId, sprintId, request);
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }
                if (result.Message.Contains("access") || result.Message.Contains("permission"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, result);
                }
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<SprintResponse>.FailResponse(ex.Message));
        }
    }

    [HttpDelete("api/sprints/{sprintId:guid}/tasks/{taskId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<SprintResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveTaskFromSprint(Guid sprintId, Guid taskId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _sprintService.RemoveTaskFromSprintAsync(userId, sprintId, taskId);
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }
                if (result.Message.Contains("access") || result.Message.Contains("permission"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, result);
                }
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<SprintResponse>.FailResponse(ex.Message));
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
