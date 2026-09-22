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
using TaskFlow.Application.DTOs.Tasks;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.API.Controllers;

[ApiController]
[Authorize]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly IValidator<CreateTaskRequest> _createTaskValidator;
    private readonly IValidator<UpdateTaskRequest> _updateTaskValidator;
    private readonly IValidator<MoveTaskRequest> _moveTaskValidator;
    private readonly IValidator<UpdateTaskStatusRequest> _updateStatusValidator;
    private readonly IValidator<UpdateTaskPriorityRequest> _updatePriorityValidator;
    private readonly IValidator<UpdateTaskDueDateRequest> _updateDueDateValidator;
    private readonly IValidator<AddDependencyRequest> _addDependencyValidator;
    private readonly IValidator<AddAssigneeRequest> _addAssigneeValidator;
    private readonly IValidator<AddWatcherRequest> _addWatcherValidator;

    public TasksController(
        ITaskService taskService,
        IValidator<CreateTaskRequest> createTaskValidator,
        IValidator<UpdateTaskRequest> updateTaskValidator,
        IValidator<MoveTaskRequest> moveTaskValidator,
        IValidator<UpdateTaskStatusRequest> updateStatusValidator,
        IValidator<UpdateTaskPriorityRequest> updatePriorityValidator,
        IValidator<UpdateTaskDueDateRequest> updateDueDateValidator,
        IValidator<AddDependencyRequest> addDependencyValidator,
        IValidator<AddAssigneeRequest> addAssigneeValidator,
        IValidator<AddWatcherRequest> addWatcherValidator)
    {
        _taskService = taskService;
        _createTaskValidator = createTaskValidator;
        _updateTaskValidator = updateTaskValidator;
        _moveTaskValidator = moveTaskValidator;
        _updateStatusValidator = updateStatusValidator;
        _updatePriorityValidator = updatePriorityValidator;
        _updateDueDateValidator = updateDueDateValidator;
        _addDependencyValidator = addDependencyValidator;
        _addAssigneeValidator = addAssigneeValidator;
        _addWatcherValidator = addWatcherValidator;
    }

    [HttpPost("api/columns/{columnId:guid}/tasks")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateTask(Guid columnId, [FromBody] CreateTaskRequest request)
    {
        var validationResult = await _createTaskValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<TaskResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _taskService.CreateTaskAsync(userId, columnId, request);
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
            return Unauthorized(ApiResponse<TaskResponse>.FailResponse(ex.Message));
        }
    }

    [HttpGet("api/boards/{boardId:guid}/tasks")]
    [ProducesResponseType(typeof(PaginatedResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(PaginatedResponse<TaskResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(PaginatedResponse<TaskResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(PaginatedResponse<TaskResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetBoardTasks(Guid boardId, [FromQuery] int limit = 100, [FromQuery] int offset = 0)
    {
        if (limit <= 0) limit = 100;
        if (offset < 0) offset = 0;

        try
        {
            var userId = GetUserId();
            var result = await _taskService.GetBoardTasksAsync(userId, boardId, limit, offset);
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
            return Unauthorized(PaginatedResponse<TaskResponse>.FailResponse(ex.Message));
        }
    }

    [HttpGet("api/tasks/{taskId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetTaskById(Guid taskId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _taskService.GetTaskByIdAsync(userId, taskId);
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
            return Unauthorized(ApiResponse<TaskResponse>.FailResponse(ex.Message));
        }
    }

    [HttpPatch("api/tasks/{taskId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTask(Guid taskId, [FromBody] UpdateTaskRequest request)
    {
        var validationResult = await _updateTaskValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<TaskResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _taskService.UpdateTaskAsync(userId, taskId, request);
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
            return Unauthorized(ApiResponse<TaskResponse>.FailResponse(ex.Message));
        }
    }

    [HttpDelete("api/tasks/{taskId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteTask(Guid taskId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _taskService.DeleteTaskAsync(userId, taskId);
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

    [HttpPost("api/tasks/{taskId:guid}/move")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MoveTask(Guid taskId, [FromBody] MoveTaskRequest request)
    {
        var validationResult = await _moveTaskValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<TaskResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _taskService.MoveTaskAsync(userId, taskId, request);
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
            return Unauthorized(ApiResponse<TaskResponse>.FailResponse(ex.Message));
        }
    }

    [HttpPost("api/tasks/{taskId:guid}/status")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateStatus(Guid taskId, [FromBody] UpdateTaskStatusRequest request)
    {
        var validationResult = await _updateStatusValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<TaskResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _taskService.UpdateStatusAsync(userId, taskId, request);
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
            return Unauthorized(ApiResponse<TaskResponse>.FailResponse(ex.Message));
        }
    }

    [HttpPost("api/tasks/{taskId:guid}/priority")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdatePriority(Guid taskId, [FromBody] UpdateTaskPriorityRequest request)
    {
        var validationResult = await _updatePriorityValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<TaskResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _taskService.UpdatePriorityAsync(userId, taskId, request);
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
            return Unauthorized(ApiResponse<TaskResponse>.FailResponse(ex.Message));
        }
    }

    [HttpPost("api/tasks/{taskId:guid}/due-date")]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<TaskResponse>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateDueDate(Guid taskId, [FromBody] UpdateTaskDueDateRequest request)
    {
        var validationResult = await _updateDueDateValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<TaskResponse>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _taskService.UpdateDueDateAsync(userId, taskId, request);
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
            return Unauthorized(ApiResponse<TaskResponse>.FailResponse(ex.Message));
        }
    }

    // Dependencies
    [HttpPost("api/tasks/{taskId:guid}/dependencies")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddDependency(Guid taskId, [FromBody] AddDependencyRequest request)
    {
        var validationResult = await _addDependencyValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _taskService.AddDependencyAsync(userId, taskId, request);
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

    [HttpDelete("api/tasks/{taskId:guid}/dependencies/{dependsOnId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveDependency(Guid taskId, Guid dependsOnId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _taskService.RemoveDependencyAsync(userId, taskId, dependsOnId);
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

    // Assignees
    [HttpPost("api/tasks/{taskId:guid}/assignees")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddAssignee(Guid taskId, [FromBody] AddAssigneeRequest request)
    {
        var validationResult = await _addAssigneeValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _taskService.AddAssigneeAsync(userId, taskId, request);
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

    [HttpDelete("api/tasks/{taskId:guid}/assignees/{assigneeUserId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveAssignee(Guid taskId, Guid assigneeUserId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _taskService.RemoveAssigneeAsync(userId, taskId, assigneeUserId);
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

    // Watchers
    [HttpPost("api/tasks/{taskId:guid}/watchers")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddWatcher(Guid taskId, [FromBody] AddWatcherRequest request)
    {
        var validationResult = await _addWatcherValidator.ValidateAsync(request);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors.Select(e => e.ErrorMessage).ToList();
            return BadRequest(ApiResponse<object>.FailResponse(string.Join(" ", errors)));
        }

        try
        {
            var userId = GetUserId();
            var result = await _taskService.AddWatcherAsync(userId, taskId, request);
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

    [HttpDelete("api/tasks/{taskId:guid}/watchers/{watcherUserId:guid}")]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> RemoveWatcher(Guid taskId, Guid watcherUserId)
    {
        try
        {
            var userId = GetUserId();
            var result = await _taskService.RemoveWatcherAsync(userId, taskId, watcherUserId);
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
