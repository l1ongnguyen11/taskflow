using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Dashboard;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.API.Controllers;

[ApiController]
[Authorize]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    [HttpGet("api/workspaces/{workspaceId:guid}/dashboard/statistics")]
    [Authorize(Policy = "workspace:read")]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceStatisticsResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceStatisticsResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<WorkspaceStatisticsResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetWorkspaceStatistics(Guid workspaceId)
    {
        var result = await _dashboardService.GetWorkspaceStatisticsAsync(GetUserId(), workspaceId);
        return MapResult(result);
    }

    [HttpGet("api/projects/{projectId:guid}/dashboard/statistics")]
    [Authorize(Policy = "project:read")]
    [ProducesResponseType(typeof(ApiResponse<ProjectStatisticsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProjectStatistics(Guid projectId)
    {
        var result = await _dashboardService.GetProjectStatisticsAsync(GetUserId(), projectId);
        return MapResult(result);
    }

    [HttpGet("api/projects/{projectId:guid}/dashboard/tasks/statistics")]
    [Authorize(Policy = "project:read")]
    [ProducesResponseType(typeof(ApiResponse<TaskStatisticsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTaskStatistics(Guid projectId)
    {
        var result = await _dashboardService.GetTaskStatisticsAsync(GetUserId(), projectId);
        return MapResult(result);
    }

    [HttpGet("api/projects/{projectId:guid}/dashboard/sprints/statistics")]
    [Authorize(Policy = "project:read")]
    [ProducesResponseType(typeof(ApiResponse<SprintStatisticsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSprintStatistics(Guid projectId)
    {
        var result = await _dashboardService.GetSprintStatisticsAsync(GetUserId(), projectId);
        return MapResult(result);
    }

    [HttpGet("api/workspaces/{workspaceId:guid}/dashboard/activities/summary")]
    [Authorize(Policy = "workspace:read")]
    [ProducesResponseType(typeof(ApiResponse<ActivitySummaryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetActivitySummary(Guid workspaceId)
    {
        var result = await _dashboardService.GetActivitySummaryAsync(GetUserId(), workspaceId);
        return MapResult(result);
    }

    [HttpGet("api/projects/{projectId:guid}/dashboard/time/statistics")]
    [Authorize(Policy = "project:read")]
    [ProducesResponseType(typeof(ApiResponse<TimeTrackingStatisticsResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProjectTimeStatistics(Guid projectId)
    {
        var result = await _dashboardService.GetProjectTimeStatisticsAsync(GetUserId(), projectId);
        return MapResult(result);
    }

    [HttpGet("api/projects/{projectId:guid}/reports")]
    [Authorize(Policy = "project:read")]
    [ProducesResponseType(typeof(ApiResponse<ProjectReportResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProjectReport(Guid projectId)
    {
        var result = await _dashboardService.GetProjectReportAsync(GetUserId(), projectId);
        return MapResult(result);
    }

    private IActionResult MapResult<T>(ApiResponse<T> result)
    {
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
