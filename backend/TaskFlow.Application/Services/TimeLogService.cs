using System;
using System.Collections.Generic;
using System.Linq;
using Task = System.Threading.Tasks.Task;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.TimeLogs;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;
using TaskFlow.Domain.Entities;

namespace TaskFlow.Application.Services;

public class TimeLogService : ITimeLogService
{
    private readonly ITimeLogRepository _timeLogRepository;
    private readonly ITaskRepository _taskRepository;
    private readonly IBoardRepository _boardRepository;
    private readonly IProjectRepository _projectRepository;
    private readonly IWorkspaceRepository _workspaceRepository;

    public TimeLogService(
        ITimeLogRepository timeLogRepository,
        ITaskRepository taskRepository,
        IBoardRepository boardRepository,
        IProjectRepository projectRepository,
        IWorkspaceRepository workspaceRepository)
    {
        _timeLogRepository = timeLogRepository;
        _taskRepository = taskRepository;
        _boardRepository = boardRepository;
        _projectRepository = projectRepository;
        _workspaceRepository = workspaceRepository;
    }

    public async Task<ApiResponse<TimeLogResponse>> StartTimerAsync(
        Guid userId, Guid taskId, StartTimerRequest request)
    {
        var accessError = await EnsureTaskAccessAsync(taskId, userId);
        if (accessError != null)
        {
            return accessError;
        }

        var activeTimer = await _timeLogRepository.GetActiveTimerByUserIdAsync(userId);
        if (activeTimer != null)
        {
            return ApiResponse<TimeLogResponse>.FailResponse("You already have a running timer. Stop it before starting a new one.");
        }

        var now = DateTime.UtcNow;
        var timeLog = new TimeLog
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            UserId = userId,
            Description = request.Description?.Trim(),
            StartedAt = now,
            EndedAt = null,
            DurationMinutes = 1,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _timeLogRepository.AddAsync(timeLog);
        await _timeLogRepository.SaveChangesAsync();

        var reloaded = await _timeLogRepository.GetByIdAsync(timeLog.Id);
        return ApiResponse<TimeLogResponse>.SuccessResponse(
            MapToTimeLogResponse(reloaded!), "Timer started successfully.");
    }

    public async Task<ApiResponse<TimeLogResponse>> StopTimerAsync(Guid userId)
    {
        var activeTimer = await _timeLogRepository.GetActiveTimerByUserIdAsync(userId);
        if (activeTimer == null)
        {
            return ApiResponse<TimeLogResponse>.FailResponse("No running timer found.");
        }

        var accessError = await EnsureTaskAccessAsync(activeTimer.TaskId, userId);
        if (accessError != null)
        {
            return accessError;
        }

        var endedAt = DateTime.UtcNow;
        var duration = (int)Math.Ceiling((endedAt - activeTimer.StartedAt).TotalMinutes);
        if (duration < 1)
        {
            duration = 1;
        }

        activeTimer.EndedAt = endedAt;
        activeTimer.DurationMinutes = duration;
        activeTimer.UpdatedAt = endedAt;

        await _timeLogRepository.SaveChangesAsync();

        return ApiResponse<TimeLogResponse>.SuccessResponse(
            MapToTimeLogResponse(activeTimer), "Timer stopped successfully.");
    }

    public async Task<ApiResponse<TimeLogResponse>> ManualLogAsync(
        Guid userId, Guid taskId, ManualLogRequest request)
    {
        var accessError = await EnsureTaskAccessAsync(taskId, userId);
        if (accessError != null)
        {
            return accessError;
        }

        var startedAt = request.StartedAt.ToUniversalTime();
        var endedAt = startedAt.AddMinutes(request.DurationMinutes);
        var now = DateTime.UtcNow;

        var timeLog = new TimeLog
        {
            Id = Guid.NewGuid(),
            TaskId = taskId,
            UserId = userId,
            Description = request.Description?.Trim(),
            StartedAt = startedAt,
            EndedAt = endedAt,
            DurationMinutes = request.DurationMinutes,
            CreatedAt = now,
            UpdatedAt = now
        };

        await _timeLogRepository.AddAsync(timeLog);
        await _timeLogRepository.SaveChangesAsync();

        var reloaded = await _timeLogRepository.GetByIdAsync(timeLog.Id);
        return ApiResponse<TimeLogResponse>.SuccessResponse(
            MapToTimeLogResponse(reloaded!), "Time log created successfully.");
    }

    public async Task<ApiResponse<TimeLogResponse?>> GetActiveTimerAsync(Guid userId)
    {
        var activeTimer = await _timeLogRepository.GetActiveTimerByUserIdAsync(userId);
        if (activeTimer == null)
        {
            return ApiResponse<TimeLogResponse?>.SuccessResponse(null, "No active timer running.");
        }
        return ApiResponse<TimeLogResponse?>.SuccessResponse(MapToTimeLogResponse(activeTimer), "Active timer retrieved.");
    }

    public async Task<ApiResponse<TimeReportResponse>> GetTimeReportAsync(
        Guid userId,
        Guid taskId,
        Guid? filterUserId,
        DateTime? from,
        DateTime? to,
        int limit,
        int offset)
    {
        var accessError = await EnsureTaskAccessAsync(taskId, userId);
        if (accessError != null)
        {
            return ApiResponse<TimeReportResponse>.FailResponse(accessError.Message);
        }

        var normalizedFrom = from?.ToUniversalTime();
        var normalizedTo = to?.ToUniversalTime();

        if (normalizedFrom.HasValue && normalizedTo.HasValue && normalizedTo < normalizedFrom)
        {
            return ApiResponse<TimeReportResponse>.FailResponse("'to' must be on or after 'from'.");
        }

        var entries = await _timeLogRepository.GetByTaskIdAsync(
            taskId, filterUserId, normalizedFrom, normalizedTo, limit, offset);
        var totalEntries = await _timeLogRepository.CountByTaskIdAsync(
            taskId, filterUserId, normalizedFrom, normalizedTo);
        var totalDuration = await _timeLogRepository.SumDurationByTaskIdAsync(
            taskId, filterUserId, normalizedFrom, normalizedTo);
        var userSummaries = await _timeLogRepository.GetUserSummariesByTaskIdAsync(
            taskId, normalizedFrom, normalizedTo);

        var report = new TimeReportResponse
        {
            TaskId = taskId,
            From = normalizedFrom,
            To = normalizedTo,
            TotalDurationMinutes = totalDuration,
            TotalEntries = totalEntries,
            ByUser = userSummaries.Select(s => new TimeReportUserSummary
            {
                UserId = s.UserId,
                UserDisplayName = s.UserDisplayName,
                TotalDurationMinutes = s.TotalMinutes,
                EntryCount = s.EntryCount
            }).ToList(),
            Entries = entries.Select(MapToTimeLogResponse).ToList(),
            Pagination = new PaginationMetadata
            {
                TotalCount = totalEntries,
                Limit = limit,
                Offset = offset
            }
        };

        return ApiResponse<TimeReportResponse>.SuccessResponse(report, "Time report retrieved successfully.");
    }

    private async Task<ApiResponse<TimeLogResponse>?> EnsureTaskAccessAsync(Guid taskId, Guid userId)
    {
        var task = await _taskRepository.GetByIdAsync(taskId);
        if (task == null)
        {
            return ApiResponse<TimeLogResponse>.FailResponse("Task not found.");
        }

        var column = await _boardRepository.GetColumnByIdAsync(task.BoardColumnId ?? Guid.Empty);
        if (column == null)
        {
            return ApiResponse<TimeLogResponse>.FailResponse("You do not have access to this task.");
        }

        var project = await _projectRepository.GetByIdAsync(column.Board.ProjectId);
        if (project == null || project.DeletedAt != null)
        {
            return ApiResponse<TimeLogResponse>.FailResponse("You do not have access to this task.");
        }

        var isMember = await _workspaceRepository.IsMemberAsync(project.WorkspaceId, userId);
        if (!isMember)
        {
            return ApiResponse<TimeLogResponse>.FailResponse("You do not have access to this task.");
        }

        return null;
    }

    private static TimeLogResponse MapToTimeLogResponse(TimeLog timeLog)
    {
        return new TimeLogResponse
        {
            Id = timeLog.Id,
            TaskId = timeLog.TaskId,
            UserId = timeLog.UserId,
            UserDisplayName = timeLog.User?.DisplayName,
            Description = timeLog.Description,
            StartedAt = timeLog.StartedAt,
            EndedAt = timeLog.EndedAt,
            DurationMinutes = timeLog.DurationMinutes,
            IsRunning = timeLog.EndedAt == null,
            CreatedAt = timeLog.CreatedAt,
            UpdatedAt = timeLog.UpdatedAt
        };
    }
}
