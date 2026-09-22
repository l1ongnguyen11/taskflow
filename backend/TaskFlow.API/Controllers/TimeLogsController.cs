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
using TaskFlow.Application.DTOs.TimeLogs;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.API.Controllers;

[ApiController]
[Authorize]
public class TimeLogsController : ControllerBase
{
    private readonly ITimeLogService _timeLogService;
    private readonly IValidator<StartTimerRequest> _startTimerValidator;
    private readonly IValidator<ManualLogRequest> _manualLogValidator;

    public TimeLogsController(
        ITimeLogService timeLogService,
        IValidator<StartTimerRequest> startTimerValidator,
        IValidator<ManualLogRequest> manualLogValidator)
    {
        _timeLogService = timeLogService;
        _startTimerValidator = startTimerValidator;
        _manualLogValidator = manualLogValidator;
    }

    [HttpPost("api/tasks/{taskId:guid}/time-logs/start")]
    [ProducesResponseType(typeof(ApiResponse<TimeLogResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<TimeLogResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TimeLogResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<TimeLogResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<TimeLogResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StartTimer(Guid taskId, [FromBody] StartTimerRequest request)
    {
        var validationResult = await _startTimerValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<TimeLogResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _timeLogService.StartTimerAsync(userId, taskId, request);
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }
                if (result.Message.Contains("access"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, result);
                }
                return BadRequest(result);
            }
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<TimeLogResponse>.FailResponse(ex.Message));
        }
    }

    [HttpPatch("api/time-logs/stop")]
    [ProducesResponseType(typeof(ApiResponse<TimeLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TimeLogResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TimeLogResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<TimeLogResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<TimeLogResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> StopTimer()
    {
        try
        {
            var userId = GetUserId();
            var result = await _timeLogService.StopTimerAsync(userId);
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }
                if (result.Message.Contains("access"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, result);
                }
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<TimeLogResponse>.FailResponse(ex.Message));
        }
    }

    [HttpGet("api/time-logs/active")]
    [ProducesResponseType(typeof(ApiResponse<TimeLogResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TimeLogResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetActiveTimer()
    {
        try
        {
            var userId = GetUserId();
            var result = await _timeLogService.GetActiveTimerAsync(userId);
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<TimeLogResponse>.FailResponse(ex.Message));
        }
    }

    [HttpPost("api/tasks/{taskId:guid}/time-logs")]
    [ProducesResponseType(typeof(ApiResponse<TimeLogResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<TimeLogResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TimeLogResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<TimeLogResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<TimeLogResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ManualLog(Guid taskId, [FromBody] ManualLogRequest request)
    {
        var validationResult = await _manualLogValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<TimeLogResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _timeLogService.ManualLogAsync(userId, taskId, request);
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }
                if (result.Message.Contains("access"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, result);
                }
                return BadRequest(result);
            }
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<TimeLogResponse>.FailResponse(ex.Message));
        }
    }

    [HttpGet("api/tasks/{taskId:guid}/time-logs/report")]
    [ProducesResponseType(typeof(ApiResponse<TimeReportResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TimeReportResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TimeReportResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<TimeReportResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<TimeReportResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTimeReport(
        Guid taskId,
        [FromQuery] Guid? userId,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0)
    {
        if (limit <= 0) limit = 20;
        if (offset < 0) offset = 0;

        try
        {
            var currentUserId = GetUserId();
            var result = await _timeLogService.GetTimeReportAsync(
                currentUserId, taskId, userId, from, to, limit, offset);
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }
                if (result.Message.Contains("access"))
                {
                    return StatusCode(StatusCodes.Status403Forbidden, result);
                }
                return BadRequest(result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<TimeReportResponse>.FailResponse(ex.Message));
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
