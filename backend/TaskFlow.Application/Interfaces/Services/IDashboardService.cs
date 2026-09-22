using System;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Dashboard;

namespace TaskFlow.Application.Interfaces.Services;

public interface IDashboardService
{
    System.Threading.Tasks.Task<ApiResponse<WorkspaceStatisticsResponse>> GetWorkspaceStatisticsAsync(Guid userId, Guid workspaceId);
    System.Threading.Tasks.Task<ApiResponse<ProjectStatisticsResponse>> GetProjectStatisticsAsync(Guid userId, Guid projectId);
    System.Threading.Tasks.Task<ApiResponse<TaskStatisticsResponse>> GetTaskStatisticsAsync(Guid userId, Guid projectId);
    System.Threading.Tasks.Task<ApiResponse<SprintStatisticsResponse>> GetSprintStatisticsAsync(Guid userId, Guid projectId);
    System.Threading.Tasks.Task<ApiResponse<ActivitySummaryResponse>> GetActivitySummaryAsync(Guid userId, Guid workspaceId);
    System.Threading.Tasks.Task<ApiResponse<TimeTrackingStatisticsResponse>> GetProjectTimeStatisticsAsync(Guid userId, Guid projectId);
    System.Threading.Tasks.Task<ApiResponse<ProjectReportResponse>> GetProjectReportAsync(Guid userId, Guid projectId);
}
