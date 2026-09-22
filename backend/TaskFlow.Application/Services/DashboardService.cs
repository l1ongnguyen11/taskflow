using System.Threading.Tasks;
using System;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Dashboard;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Application.Interfaces.Services;

namespace TaskFlow.Application.Services;

public class DashboardService : IDashboardService
{
    private readonly IDashboardRepository _dashboardRepository;
    private readonly IWorkspaceRepository _workspaceRepository;
    private readonly IProjectRepository _projectRepository;

    public DashboardService(
        IDashboardRepository dashboardRepository,
        IWorkspaceRepository workspaceRepository,
        IProjectRepository projectRepository)
    {
        _dashboardRepository = dashboardRepository;
        _workspaceRepository = workspaceRepository;
        _projectRepository = projectRepository;
    }

    public async Task<ApiResponse<WorkspaceStatisticsResponse>> GetWorkspaceStatisticsAsync(
        Guid userId, Guid workspaceId)
    {
        var accessError = await EnsureWorkspaceMemberAsync(userId, workspaceId);
        if (accessError != null)
        {
            return ApiResponse<WorkspaceStatisticsResponse>.FailResponse(accessError);
        }

        var stats = await _dashboardRepository.GetWorkspaceStatisticsAsync(workspaceId, userId);
        return ApiResponse<WorkspaceStatisticsResponse>.SuccessResponse(stats, "Workspace statistics retrieved successfully.");
    }

    public async Task<ApiResponse<ProjectStatisticsResponse>> GetProjectStatisticsAsync(Guid userId, Guid projectId)
    {
        var accessError = await EnsureProjectMemberAsync(userId, projectId);
        if (accessError != null)
        {
            return ApiResponse<ProjectStatisticsResponse>.FailResponse(accessError);
        }

        var stats = await _dashboardRepository.GetProjectStatisticsAsync(projectId);
        return ApiResponse<ProjectStatisticsResponse>.SuccessResponse(stats, "Project statistics retrieved successfully.");
    }

    public async Task<ApiResponse<TaskStatisticsResponse>> GetTaskStatisticsAsync(Guid userId, Guid projectId)
    {
        var accessError = await EnsureProjectMemberAsync(userId, projectId);
        if (accessError != null)
        {
            return ApiResponse<TaskStatisticsResponse>.FailResponse(accessError);
        }

        var stats = await _dashboardRepository.GetTaskStatisticsAsync(projectId);
        return ApiResponse<TaskStatisticsResponse>.SuccessResponse(stats, "Task statistics retrieved successfully.");
    }

    public async Task<ApiResponse<SprintStatisticsResponse>> GetSprintStatisticsAsync(Guid userId, Guid projectId)
    {
        var accessError = await EnsureProjectMemberAsync(userId, projectId);
        if (accessError != null)
        {
            return ApiResponse<SprintStatisticsResponse>.FailResponse(accessError);
        }

        var stats = await _dashboardRepository.GetSprintStatisticsAsync(projectId);
        return ApiResponse<SprintStatisticsResponse>.SuccessResponse(stats, "Sprint statistics retrieved successfully.");
    }

    public async Task<ApiResponse<ActivitySummaryResponse>> GetActivitySummaryAsync(Guid userId, Guid workspaceId)
    {
        var accessError = await EnsureWorkspaceMemberAsync(userId, workspaceId);
        if (accessError != null)
        {
            return ApiResponse<ActivitySummaryResponse>.FailResponse(accessError);
        }

        var summary = await _dashboardRepository.GetActivitySummaryAsync(workspaceId);
        return ApiResponse<ActivitySummaryResponse>.SuccessResponse(summary, "Activity summary retrieved successfully.");
    }

    public async Task<ApiResponse<TimeTrackingStatisticsResponse>> GetProjectTimeStatisticsAsync(Guid userId, Guid projectId)
    {
        var accessError = await EnsureProjectMemberAsync(userId, projectId);
        if (accessError != null)
        {
            return ApiResponse<TimeTrackingStatisticsResponse>.FailResponse(accessError);
        }

        var stats = await _dashboardRepository.GetProjectTimeStatisticsAsync(projectId);
        return ApiResponse<TimeTrackingStatisticsResponse>.SuccessResponse(stats, "Project time tracking statistics retrieved successfully.");
    }

    public async Task<ApiResponse<ProjectReportResponse>> GetProjectReportAsync(Guid userId, Guid projectId)
    {
        var accessError = await EnsureProjectMemberAsync(userId, projectId);
        if (accessError != null)
        {
            return ApiResponse<ProjectReportResponse>.FailResponse(accessError);
        }

        var report = await _dashboardRepository.GetProjectReportAsync(projectId);
        return ApiResponse<ProjectReportResponse>.SuccessResponse(report, "Project report retrieved successfully.");
    }

    private async Task<string?> EnsureWorkspaceMemberAsync(Guid userId, Guid workspaceId)
    {
        var workspace = await _workspaceRepository.GetByIdAsync(workspaceId);
        if (workspace == null)
        {
            return "Workspace not found.";
        }

        if (!await _workspaceRepository.IsMemberAsync(workspaceId, userId))
        {
            return "You do not have access to this workspace.";
        }

        return null;
    }

    private async Task<string?> EnsureProjectMemberAsync(Guid userId, Guid projectId)
    {
        var project = await _projectRepository.GetByIdAsync(projectId);
        if (project == null || project.DeletedAt != null)
        {
            return "Project not found.";
        }

        if (!await _workspaceRepository.IsMemberAsync(project.WorkspaceId, userId))
        {
            return "You do not have access to this project.";
        }

        return null;
    }
}
