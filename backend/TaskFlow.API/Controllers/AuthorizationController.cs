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
using TaskFlow.Application.DTOs.Authorization;
using TaskFlow.Application.Interfaces.Services;
using IAppAuthorizationService = TaskFlow.Application.Interfaces.Services.IAuthorizationService;

namespace TaskFlow.API.Controllers;

[ApiController]
[Authorize]
public class AuthorizationController : ControllerBase
{
    private readonly IAppAuthorizationService _authorizationService;
    private readonly IValidator<CreateRoleRequest> _createRoleValidator;
    private readonly IValidator<UpdateRoleRequest> _updateRoleValidator;
    private readonly IValidator<CreatePermissionRequest> _createPermissionValidator;
    private readonly IValidator<UpdatePermissionRequest> _updatePermissionValidator;
    private readonly IValidator<AssignUserRoleRequest> _assignUserRoleValidator;
    private readonly IValidator<AssignRolePermissionRequest> _assignRolePermissionValidator;

    public AuthorizationController(
        IAppAuthorizationService authorizationService,
        IValidator<CreateRoleRequest> createRoleValidator,
        IValidator<UpdateRoleRequest> updateRoleValidator,
        IValidator<CreatePermissionRequest> createPermissionValidator,
        IValidator<UpdatePermissionRequest> updatePermissionValidator,
        IValidator<AssignUserRoleRequest> assignUserRoleValidator,
        IValidator<AssignRolePermissionRequest> assignRolePermissionValidator)
    {
        _authorizationService = authorizationService;
        _createRoleValidator = createRoleValidator;
        _updateRoleValidator = updateRoleValidator;
        _createPermissionValidator = createPermissionValidator;
        _updatePermissionValidator = updatePermissionValidator;
        _assignUserRoleValidator = assignUserRoleValidator;
        _assignRolePermissionValidator = assignRolePermissionValidator;
    }

    [HttpGet("api/roles")]
    [ProducesResponseType(typeof(ApiResponse<List<RoleResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRoles()
    {
        var result = await _authorizationService.GetRolesAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("api/roles/{roleId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRoleById(Guid roleId)
    {
        var result = await _authorizationService.GetRoleByIdAsync(GetUserId(), roleId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("api/roles")]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreateRole([FromBody] CreateRoleRequest request)
    {
        var validation = await _createRoleValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<RoleResponse>.FailResponse(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage))));
        }

        var result = await _authorizationService.CreateRoleAsync(GetUserId(), request);
        if (!result.Success)
        {
            return result.Message.Contains("permission") ? StatusCode(StatusCodes.Status403Forbidden, result) : BadRequest(result);
        }
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPatch("api/roles/{roleId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<RoleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdateRole(Guid roleId, [FromBody] UpdateRoleRequest request)
    {
        var validation = await _updateRoleValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<RoleResponse>.FailResponse(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage))));
        }

        var result = await _authorizationService.UpdateRoleAsync(GetUserId(), roleId, request);
        if (!result.Success)
        {
            if (result.Message.Contains("not found")) return NotFound(result);
            if (result.Message.Contains("permission")) return StatusCode(StatusCodes.Status403Forbidden, result);
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpDelete("api/roles/{roleId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteRole(Guid roleId)
    {
        var result = await _authorizationService.DeleteRoleAsync(GetUserId(), roleId);
        if (!result.Success)
        {
            if (result.Message.Contains("not found")) return NotFound(result);
            if (result.Message.Contains("permission")) return StatusCode(StatusCodes.Status403Forbidden, result);
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("api/permissions")]
    [ProducesResponseType(typeof(ApiResponse<List<PermissionResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissions()
    {
        var result = await _authorizationService.GetPermissionsAsync(GetUserId());
        return Ok(result);
    }

    [HttpGet("api/permissions/{permissionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PermissionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetPermissionById(Guid permissionId)
    {
        var result = await _authorizationService.GetPermissionByIdAsync(GetUserId(), permissionId);
        return result.Success ? Ok(result) : NotFound(result);
    }

    [HttpPost("api/permissions")]
    [ProducesResponseType(typeof(ApiResponse<PermissionResponse>), StatusCodes.Status201Created)]
    public async Task<IActionResult> CreatePermission([FromBody] CreatePermissionRequest request)
    {
        var validation = await _createPermissionValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<PermissionResponse>.FailResponse(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage))));
        }

        var result = await _authorizationService.CreatePermissionAsync(GetUserId(), request);
        if (!result.Success)
        {
            return result.Message.Contains("permission") ? StatusCode(StatusCodes.Status403Forbidden, result) : BadRequest(result);
        }
        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPatch("api/permissions/{permissionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<PermissionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> UpdatePermission(Guid permissionId, [FromBody] UpdatePermissionRequest request)
    {
        var validation = await _updatePermissionValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<PermissionResponse>.FailResponse(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage))));
        }

        var result = await _authorizationService.UpdatePermissionAsync(GetUserId(), permissionId, request);
        if (!result.Success)
        {
            if (result.Message.Contains("not found")) return NotFound(result);
            if (result.Message.Contains("permission")) return StatusCode(StatusCodes.Status403Forbidden, result);
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpDelete("api/permissions/{permissionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeletePermission(Guid permissionId)
    {
        var result = await _authorizationService.DeletePermissionAsync(GetUserId(), permissionId);
        if (!result.Success)
        {
            if (result.Message.Contains("not found")) return NotFound(result);
            if (result.Message.Contains("permission")) return StatusCode(StatusCodes.Status403Forbidden, result);
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("api/workspaces/{workspaceId:guid}/user-roles")]
    [Authorize(Policy = "workspace:read")]
    [ProducesResponseType(typeof(ApiResponse<List<UserRoleResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetWorkspaceUserRoles(Guid workspaceId)
    {
        var result = await _authorizationService.GetWorkspaceUserRolesAsync(GetUserId(), workspaceId);
        if (!result.Success)
        {
            if (result.Message.Contains("not found")) return NotFound(result);
            return StatusCode(StatusCodes.Status403Forbidden, result);
        }
        return Ok(result);
    }

    [HttpPost("api/workspaces/{workspaceId:guid}/user-roles")]
    [Authorize(Policy = "member:manage")]
    [ProducesResponseType(typeof(ApiResponse<UserRoleResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignUserRole(Guid workspaceId, [FromBody] AssignUserRoleRequest request)
    {
        var validation = await _assignUserRoleValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<UserRoleResponse>.FailResponse(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage))));
        }

        var result = await _authorizationService.AssignUserRoleAsync(GetUserId(), workspaceId, request);
        if (!result.Success)
        {
            if (result.Message.Contains("not found")) return NotFound(result);
            if (result.Message.Contains("permission")) return StatusCode(StatusCodes.Status403Forbidden, result);
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpDelete("api/workspaces/{workspaceId:guid}/user-roles/{targetUserId:guid}/roles/{roleId:guid}")]
    [Authorize(Policy = "member:manage")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveUserRole(Guid workspaceId, Guid targetUserId, Guid roleId)
    {
        var result = await _authorizationService.RemoveUserRoleAsync(GetUserId(), workspaceId, targetUserId, roleId);
        if (!result.Success)
        {
            if (result.Message.Contains("not found")) return NotFound(result);
            if (result.Message.Contains("permission")) return StatusCode(StatusCodes.Status403Forbidden, result);
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpGet("api/workspaces/{workspaceId:guid}/my-permissions")]
    [Authorize(Policy = "workspace:read")]
    [ProducesResponseType(typeof(ApiResponse<List<string>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyPermissions(Guid workspaceId)
    {
        var result = await _authorizationService.GetMyPermissionsAsync(GetUserId(), workspaceId);
        if (!result.Success)
        {
            if (result.Message.Contains("not found")) return NotFound(result);
            return StatusCode(StatusCodes.Status403Forbidden, result);
        }
        return Ok(result);
    }

    [HttpGet("api/roles/{roleId:guid}/permissions")]
    [ProducesResponseType(typeof(ApiResponse<List<RolePermissionResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetRolePermissions(Guid roleId)
    {
        var result = await _authorizationService.GetRolePermissionsAsync(GetUserId(), roleId);
        if (!result.Success)
        {
            if (result.Message.Contains("not found")) return NotFound(result);
            return StatusCode(StatusCodes.Status403Forbidden, result);
        }
        return Ok(result);
    }

    [HttpPost("api/roles/{roleId:guid}/permissions")]
    [ProducesResponseType(typeof(ApiResponse<RolePermissionResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> AssignRolePermission(Guid roleId, [FromBody] AssignRolePermissionRequest request)
    {
        var validation = await _assignRolePermissionValidator.ValidateAsync(request);
        if (!validation.IsValid)
        {
            return BadRequest(ApiResponse<RolePermissionResponse>.FailResponse(string.Join(" ", validation.Errors.Select(e => e.ErrorMessage))));
        }

        var result = await _authorizationService.AssignRolePermissionAsync(GetUserId(), roleId, request);
        if (!result.Success)
        {
            if (result.Message.Contains("not found")) return NotFound(result);
            if (result.Message.Contains("permission")) return StatusCode(StatusCodes.Status403Forbidden, result);
            return BadRequest(result);
        }
        return Ok(result);
    }

    [HttpDelete("api/roles/{roleId:guid}/permissions/{permissionId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> RemoveRolePermission(Guid roleId, Guid permissionId)
    {
        var result = await _authorizationService.RemoveRolePermissionAsync(GetUserId(), roleId, permissionId);
        if (!result.Success)
        {
            if (result.Message.Contains("not found")) return NotFound(result);
            return StatusCode(StatusCodes.Status403Forbidden, result);
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
