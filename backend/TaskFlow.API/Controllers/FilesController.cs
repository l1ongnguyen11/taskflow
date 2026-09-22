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
using TaskFlow.Application.DTOs.Files;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.API.Controllers;

[ApiController]
[Authorize]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly IValidator<AttachFileToTaskRequest> _attachFileValidator;

    public FilesController(
        IFileService fileService,
        IValidator<AttachFileToTaskRequest> attachFileValidator)
    {
        _fileService = fileService;
        _attachFileValidator = attachFileValidator;
    }

    [HttpPost("api/files/upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<FileResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<FileResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<FileResponse>), StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Upload(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse<FileResponse>.FailResponse("No file uploaded."));
        }

        try
        {
            var userId = GetUserId();
            await using var stream = file.OpenReadStream();
            var result = await _fileService.UploadAsync(userId, stream, file.FileName, file.ContentType, file.Length);
            if (!result.Success)
            {
                return BadRequest(result);
            }
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<FileResponse>.FailResponse(ex.Message));
        }
    }

    [HttpGet("api/files/{fileId:guid}/download")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<FileResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<FileResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<FileResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Download(Guid fileId)
    {
        try
        {
            var userId = GetUserId();
            var (error, stream, contentType, fileName) = await _fileService.DownloadAsync(userId, fileId);
            if (error != null)
            {
                if (error.Message.Contains("not found"))
                {
                    return NotFound(error);
                }
                return StatusCode(StatusCodes.Status403Forbidden, error);
            }
            return File(stream!, contentType!, fileName);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<FileResponse>.FailResponse(ex.Message));
        }
    }

    [HttpDelete("api/files/{fileId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid fileId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _fileService.DeleteAsync(userId, fileId);
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

    [HttpPost("api/tasks/{taskId:guid}/attachments")]
    [ProducesResponseType(typeof(ApiResponse<TaskAttachmentResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TaskAttachmentResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TaskAttachmentResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<TaskAttachmentResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<TaskAttachmentResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AttachToTask(Guid taskId, [FromBody] AttachFileToTaskRequest request)
    {
        var validationResult = await _attachFileValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<TaskAttachmentResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _fileService.AttachToTaskAsync(userId, taskId, request);
            if (!result.Success)
            {
                if (result.Message.Contains("not found"))
                {
                    return NotFound(result);
                }
                if (result.Message.Contains("already attached"))
                {
                    return BadRequest(result);
                }
                return StatusCode(StatusCodes.Status403Forbidden, result);
            }
            return Ok(result);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unauthorized(ApiResponse<TaskAttachmentResponse>.FailResponse(ex.Message));
        }
    }

    [HttpGet("api/tasks/{taskId:guid}/attachments")]
    [ProducesResponseType(typeof(ApiResponse<List<TaskAttachmentResponse>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<List<TaskAttachmentResponse>>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<List<TaskAttachmentResponse>>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<List<TaskAttachmentResponse>>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTaskAttachments(Guid taskId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _fileService.GetTaskAttachmentsAsync(userId, taskId);
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
            return Unauthorized(ApiResponse<List<TaskAttachmentResponse>>.FailResponse(ex.Message));
        }
    }

    [HttpDelete("api/tasks/{taskId:guid}/attachments/{attachmentId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DetachFromTask(Guid taskId, Guid attachmentId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _fileService.DetachFromTaskAsync(userId, taskId, attachmentId);
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
