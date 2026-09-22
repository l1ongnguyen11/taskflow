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
using TaskFlow.Application.DTOs.Projects;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.API.Controllers;

[ApiController]
[Authorize]
public class ProjectsController : ControllerBase
{
    private readonly IProjectService _projectService;
    private readonly IValidator<CreateProjectRequest> _createProjectValidator;
    private readonly IValidator<UpdateProjectRequest> _updateProjectValidator;
    private readonly IValidator<AddProjectMemberRequest> _addProjectMemberValidator;
    private readonly IValidator<UpdateProjectMemberRoleRequest> _updateProjectMemberRoleValidator;

    public ProjectsController(
        IProjectService projectService,
        IValidator<CreateProjectRequest> createProjectValidator,
        IValidator<UpdateProjectRequest> updateProjectValidator,
        IValidator<AddProjectMemberRequest> addProjectMemberValidator,
        IValidator<UpdateProjectMemberRoleRequest> updateProjectMemberRoleValidator)
    {
        _projectService = projectService;
        _createProjectValidator = createProjectValidator;
        _updateProjectValidator = updateProjectValidator;
        _addProjectMemberValidator = addProjectMemberValidator;
        _updateProjectMemberRoleValidator = updateProjectMemberRoleValidator;
    }

    [HttpPost("api/workspaces/{workspaceId:guid}/projects")]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateProject(Guid workspaceId, [FromBody] CreateProjectRequest request)
    {
        var validationResult = await _createProjectValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<ProjectResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _projectService.CreateProjectAsync(userId, workspaceId, request);
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
            return Unauthorized(ApiResponse<ProjectResponse>.FailResponse(ex.Message));
        }
    }

    [HttpGet("api/workspaces/{workspaceId:guid}/projects")]
    [ProducesResponseType(typeof(PaginatedResponse<ProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginatedResponse<ProjectResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(PaginatedResponse<ProjectResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetWorkspaceProjects(
        Guid workspaceId,
        [FromQuery] bool includeArchived = false,
        [FromQuery] bool includeDeleted = false,
        [FromQuery] int limit = 20,
        [FromQuery] int offset = 0)
    {
        if (limit <= 0) limit = 20;
        if (offset < 0) offset = 0;

        try
        {
            var userId = GetUserId();
            var result = await _projectService.GetWorkspaceProjectsAsync(userId, workspaceId, includeArchived, includeDeleted, limit, offset);
            if (!result.Success)
            {
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(PaginatedResponse<ProjectResponse>.FailResponse(ex.Message));
        }
    }

    [HttpGet("api/projects/{projectId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectById(Guid projectId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _projectService.GetProjectByIdAsync(userId, projectId);
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
            return Unauthorized(ApiResponse<ProjectResponse>.FailResponse(ex.Message));
        }
    }

    [HttpPatch("api/projects/{projectId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ProjectResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProject(Guid projectId, [FromBody] UpdateProjectRequest request)
    {
        var validationResult = await _updateProjectValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<ProjectResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _projectService.UpdateProjectAsync(userId, projectId, request);
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
            return Unauthorized(ApiResponse<ProjectResponse>.FailResponse(ex.Message));
        }
    }

    [HttpDelete("api/projects/{projectId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProject(Guid projectId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _projectService.DeleteProjectAsync(userId, projectId);
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return NoContent();
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    [HttpPost("api/projects/{projectId:guid}/archive")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ArchiveProject(Guid projectId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _projectService.ArchiveProjectAsync(userId, projectId);
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

    [HttpPost("api/projects/{projectId:guid}/restore")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RestoreProject(Guid projectId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _projectService.RestoreProjectAsync(userId, projectId);
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

    [HttpPost("api/projects/{projectId:guid}/members")]
    [ProducesResponseType(typeof(ApiResponse<ProjectMemberResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProjectMemberResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProjectMemberResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ProjectMemberResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ProjectMemberResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddMember(Guid projectId, [FromBody] AddProjectMemberRequest request)
    {
        var validationResult = await _addProjectMemberValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<ProjectMemberResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _projectService.AddMemberAsync(userId, projectId, request);
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
            return Unauthorized(ApiResponse<ProjectMemberResponse>.FailResponse(ex.Message));
        }
    }

    [HttpDelete("api/projects/{projectId:guid}/members/{targetUserId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveMember(Guid projectId, Guid targetUserId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _projectService.RemoveMemberAsync(userId, projectId, targetUserId);
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

    [HttpPatch("api/projects/{projectId:guid}/members/{targetUserId:guid}/role")]
    [ProducesResponseType(typeof(ApiResponse<ProjectMemberResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ProjectMemberResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<ProjectMemberResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<ProjectMemberResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<ProjectMemberResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateMemberRole(Guid projectId, Guid targetUserId, [FromBody] UpdateProjectMemberRoleRequest request)
    {
        var validationResult = await _updateProjectMemberRoleValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<ProjectMemberResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _projectService.UpdateMemberRoleAsync(userId, projectId, targetUserId, request);
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
            return Unauthorized(ApiResponse<ProjectMemberResponse>.FailResponse(ex.Message));
        }
    }

    [HttpGet("api/projects/{projectId:guid}/members")]
    [ProducesResponseType(typeof(PaginatedResponse<ProjectMemberResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginatedResponse<ProjectMemberResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(PaginatedResponse<ProjectMemberResponse>), StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> GetProjectMembers(Guid projectId, [FromQuery] int limit = 20, [FromQuery] int offset = 0)
    {
        if (limit <= 0) limit = 20;
        if (offset < 0) offset = 0;

        try
        {
            var userId = GetUserId();
            var result = await _projectService.GetProjectMembersAsync(userId, projectId, limit, offset);
            if (!result.Success)
            {
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(PaginatedResponse<ProjectMemberResponse>.FailResponse(ex.Message));
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
