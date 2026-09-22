using System;
using System.Collections.Generic;
using TaskFlow.Application.DTOs.Dashboard;

namespace TaskFlow.Application.Interfaces.Repositories;

public interface IDashboardRepository
{
    System.Threading.Tasks.Task<WorkspaceStatisticsResponse> GetWorkspaceStatisticsAsync(Guid workspaceId, Guid userId);
    System.Threading.Tasks.Task<ProjectStatisticsResponse> GetProjectStatisticsAsync(Guid projectId);
    System.Threading.Tasks.Task<TaskStatisticsResponse> GetTaskStatisticsAsync(Guid projectId);
    System.Threading.Tasks.Task<SprintStatisticsResponse> GetSprintStatisticsAsync(Guid projectId);
    System.Threading.Tasks.Task<ActivitySummaryResponse> GetActivitySummaryAsync(Guid workspaceId);
    System.Threading.Tasks.Task<TimeTrackingStatisticsResponse> GetProjectTimeStatisticsAsync(Guid projectId);
    System.Threading.Tasks.Task<ProjectReportResponse> GetProjectReportAsync(Guid projectId);
}
