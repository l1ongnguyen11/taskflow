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
using TaskFlow.Application.DTOs.Checklists;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.API.Controllers;

[ApiController]
[Authorize]
public class ChecklistsController : ControllerBase
{
    private readonly IChecklistService _checklistService;
    private readonly IValidator<CreateChecklistRequest> _createChecklistValidator;
    private readonly IValidator<UpdateChecklistRequest> _updateChecklistValidator;
    private readonly IValidator<CreateChecklistItemRequest> _createItemValidator;
    private readonly IValidator<UpdateChecklistItemRequest> _updateItemValidator;

    public ChecklistsController(
        IChecklistService checklistService,
        IValidator<CreateChecklistRequest> createChecklistValidator,
        IValidator<UpdateChecklistRequest> updateChecklistValidator,
        IValidator<CreateChecklistItemRequest> createItemValidator,
        IValidator<UpdateChecklistItemRequest> updateItemValidator)
    {
        _checklistService = checklistService;
        _createChecklistValidator = createChecklistValidator;
        _updateChecklistValidator = updateChecklistValidator;
        _createItemValidator = createItemValidator;
        _updateItemValidator = updateItemValidator;
    }

    // ========== Checklist CRUD ==========

    [HttpPost("api/tasks/{taskId:guid}/checklists")]
    [ProducesResponseType(typeof(ApiResponse<ChecklistResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ChecklistResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateChecklist(Guid taskId, [FromBody] CreateChecklistRequest request)
    {
        var validationResult = await _createChecklistValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<ChecklistResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _checklistService.CreateChecklistAsync(userId, taskId, request);
            if (!result.Success)
            {
                if (result.Message.Contains("not found")) return NotFound(result);
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<ChecklistResponse>.FailResponse(ex.Message));
        }
    }

    [HttpGet("api/tasks/{taskId:guid}/checklists")]
    [ProducesResponseType(typeof(ApiResponse<System.Collections.Generic.List<ChecklistResponse>>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChecklistsByTaskId(Guid taskId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _checklistService.GetChecklistsByTaskIdAsync(userId, taskId);
            if (!result.Success)
            {
                if (result.Message.Contains("not found")) return NotFound(result);
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    [HttpGet("api/checklists/{checklistId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ChecklistResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetChecklistById(Guid checklistId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _checklistService.GetChecklistByIdAsync(userId, checklistId);
            if (!result.Success)
            {
                if (result.Message.Contains("not found")) return NotFound(result);
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<ChecklistResponse>.FailResponse(ex.Message));
        }
    }

    [HttpPatch("api/checklists/{checklistId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ChecklistResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ChecklistResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateChecklist(Guid checklistId, [FromBody] UpdateChecklistRequest request)
    {
        var validationResult = await _updateChecklistValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<ChecklistResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _checklistService.UpdateChecklistAsync(userId, checklistId, request);
            if (!result.Success)
            {
                if (result.Message.Contains("not found")) return NotFound(result);
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<ChecklistResponse>.FailResponse(ex.Message));
        }
    }

    [HttpDelete("api/checklists/{checklistId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteChecklist(Guid checklistId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _checklistService.DeleteChecklistAsync(userId, checklistId);
            if (!result.Success)
            {
                if (result.Message.Contains("not found")) return NotFound(result);
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    // ========== Checklist Item CRUD ==========

    [HttpPost("api/checklists/{checklistId:guid}/items")]
    [ProducesResponseType(typeof(ApiResponse<ChecklistItemResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<ChecklistItemResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateItem(Guid checklistId, [FromBody] CreateChecklistItemRequest request)
    {
        var validationResult = await _createItemValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<ChecklistItemResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _checklistService.CreateItemAsync(userId, checklistId, request);
            if (!result.Success)
            {
                if (result.Message.Contains("not found")) return NotFound(result);
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<ChecklistItemResponse>.FailResponse(ex.Message));
        }
    }

    [HttpPatch("api/checklist-items/{itemId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<ChecklistItemResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<ChecklistItemResponse>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UpdateItem(Guid itemId, [FromBody] UpdateChecklistItemRequest request)
    {
        var validationResult = await _updateItemValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<ChecklistItemResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _checklistService.UpdateItemAsync(userId, itemId, request);
            if (!result.Success)
            {
                if (result.Message.Contains("not found")) return NotFound(result);
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<ChecklistItemResponse>.FailResponse(ex.Message));
        }
    }

    [HttpDelete("api/checklist-items/{itemId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    public async Task<IActionResult> DeleteItem(Guid itemId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _checklistService.DeleteItemAsync(userId, itemId);
            if (!result.Success)
            {
                if (result.Message.Contains("not found")) return NotFound(result);
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<object>.FailResponse(ex.Message));
        }
    }

    // ========== Toggle Completion ==========

    [HttpPost("api/checklist-items/{itemId:guid}/toggle")]
    [ProducesResponseType(typeof(ApiResponse<ChecklistItemResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ToggleItemCompletion(Guid itemId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _checklistService.ToggleItemCompletionAsync(userId, itemId);
            if (!result.Success)
            {
                if (result.Message.Contains("not found")) return NotFound(result);
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<ChecklistItemResponse>.FailResponse(ex.Message));
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
