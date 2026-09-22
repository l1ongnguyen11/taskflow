using System;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.TimeLogs;

namespace TaskFlow.Application.Interfaces.Services;

public interface ITimeLogService
{
    System.Threading.Tasks.Task<ApiResponse<TimeLogResponse>> StartTimerAsync(
        Guid userId, Guid taskId, StartTimerRequest request);
    System.Threading.Tasks.Task<ApiResponse<TimeLogResponse>> StopTimerAsync(Guid userId);
    System.Threading.Tasks.Task<ApiResponse<TimeLogResponse>> ManualLogAsync(
        Guid userId, Guid taskId, ManualLogRequest request);
    System.Threading.Tasks.Task<ApiResponse<TimeLogResponse?>> GetActiveTimerAsync(Guid userId);
    System.Threading.Tasks.Task<ApiResponse<TimeReportResponse>> GetTimeReportAsync(
        Guid userId,
        Guid taskId,
        Guid? filterUserId,
        DateTime? from,
        DateTime? to,
        int limit,
        int offset);
}
