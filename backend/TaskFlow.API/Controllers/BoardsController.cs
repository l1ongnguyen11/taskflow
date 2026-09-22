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
using TaskFlow.Application.DTOs.Boards;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.API.Controllers;

[ApiController]
[Authorize]
public class BoardsController : ControllerBase
{
    private readonly IBoardService _boardService;
    private readonly IValidator<CreateBoardRequest> _createBoardValidator;
    private readonly IValidator<UpdateBoardRequest> _updateBoardValidator;
    private readonly IValidator<CreateBoardColumnRequest> _createColumnValidator;
    private readonly IValidator<UpdateBoardColumnRequest> _updateColumnValidator;
    private readonly IValidator<ReorderColumnsRequest> _reorderColumnsValidator;

    public BoardsController(
        IBoardService boardService,
        IValidator<CreateBoardRequest> createBoardValidator,
        IValidator<UpdateBoardRequest> updateBoardValidator,
        IValidator<CreateBoardColumnRequest> createColumnValidator,
        IValidator<UpdateBoardColumnRequest> updateColumnValidator,
        IValidator<ReorderColumnsRequest> reorderColumnsValidator)
    {
        _boardService = boardService;
        _createBoardValidator = createBoardValidator;
        _updateBoardValidator = updateBoardValidator;
        _createColumnValidator = createColumnValidator;
        _updateColumnValidator = updateColumnValidator;
        _reorderColumnsValidator = reorderColumnsValidator;
    }

    [HttpPost("api/projects/{projectId:guid}/boards")]
    [ProducesResponseType(typeof(ApiResponse<BoardResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<BoardResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BoardResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<BoardResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<BoardResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateBoard(Guid projectId, [FromBody] CreateBoardRequest request)
    {
        var validationResult = await _createBoardValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<BoardResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _boardService.CreateBoardAsync(userId, projectId, request);
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
            return Unauthorized(ApiResponse<BoardResponse>.FailResponse(ex.Message));
        }
    }

    [HttpGet("api/projects/{projectId:guid}/boards")]
    [ProducesResponseType(typeof(PaginatedResponse<BoardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginatedResponse<BoardResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(PaginatedResponse<BoardResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(PaginatedResponse<BoardResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProjectBoards(Guid projectId, [FromQuery] int limit = 20, [FromQuery] int offset = 0)
    {
        if (limit <= 0) limit = 20;
        if (offset < 0) offset = 0;

        try
        {
            var userId = GetUserId();
            var result = await _boardService.GetProjectBoardsAsync(userId, projectId, limit, offset);
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
            return Unauthorized(PaginatedResponse<BoardResponse>.FailResponse(ex.Message));
        }
    }

    [HttpGet("api/boards/{boardId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BoardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BoardResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<BoardResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<BoardResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBoardById(Guid boardId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _boardService.GetBoardByIdAsync(userId, boardId);
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
            return Unauthorized(ApiResponse<BoardResponse>.FailResponse(ex.Message));
        }
    }

    [HttpPatch("api/boards/{boardId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BoardResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BoardResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BoardResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<BoardResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<BoardResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateBoard(Guid boardId, [FromBody] UpdateBoardRequest request)
    {
        var validationResult = await _updateBoardValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<BoardResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _boardService.UpdateBoardAsync(userId, boardId, request);
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
            return Unauthorized(ApiResponse<BoardResponse>.FailResponse(ex.Message));
        }
    }

    [HttpDelete("api/boards/{boardId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteBoard(Guid boardId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _boardService.DeleteBoardAsync(userId, boardId);
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

    // Board Columns
    [HttpPost("api/boards/{boardId:guid}/columns")]
    [ProducesResponseType(typeof(ApiResponse<BoardColumnResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BoardColumnResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BoardColumnResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<BoardColumnResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<BoardColumnResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateColumn(Guid boardId, [FromBody] CreateBoardColumnRequest request)
    {
        var validationResult = await _createColumnValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<BoardColumnResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _boardService.CreateColumnAsync(userId, boardId, request);
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
            return Unauthorized(ApiResponse<BoardColumnResponse>.FailResponse(ex.Message));
        }
    }

    [HttpGet("api/columns/{columnId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BoardColumnResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BoardColumnResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<BoardColumnResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<BoardColumnResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetColumnById(Guid columnId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _boardService.GetColumnByIdAsync(userId, columnId);
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
            return Unauthorized(ApiResponse<BoardColumnResponse>.FailResponse(ex.Message));
        }
    }

    [HttpPatch("api/columns/{columnId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<BoardColumnResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<BoardColumnResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<BoardColumnResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<BoardColumnResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<BoardColumnResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateColumn(Guid columnId, [FromBody] UpdateBoardColumnRequest request)
    {
        var validationResult = await _updateColumnValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<BoardColumnResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _boardService.UpdateColumnAsync(userId, columnId, request);
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
            return Unauthorized(ApiResponse<BoardColumnResponse>.FailResponse(ex.Message));
        }
    }

    [HttpDelete("api/columns/{columnId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteColumn(Guid columnId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _boardService.DeleteColumnAsync(userId, columnId);
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

    [HttpPost("api/boards/{boardId:guid}/columns/reorder")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ReorderColumns(Guid boardId, [FromBody] ReorderColumnsRequest request)
    {
        var validationResult = await _reorderColumnsValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _boardService.ReorderColumnsAsync(userId, boardId, request);
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
