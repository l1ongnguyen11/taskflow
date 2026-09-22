using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using TaskFlow.Application.DTOs.Dashboard;
using TaskFlow.Application.Interfaces.Repositories;
using TaskFlow.Infrastructure.Data;

namespace TaskFlow.Infrastructure.Repositories;

public class DashboardRepository : IDashboardRepository
{
    private readonly TaskFlowDbContext _context;

    public DashboardRepository(TaskFlowDbContext context)
    {
        _context = context;
    }

    public async System.Threading.Tasks.Task<WorkspaceStatisticsResponse> GetWorkspaceStatisticsAsync(
        Guid workspaceId, Guid userId)
    {
        var memberCount = await _context.WorkspaceMembers
            .CountAsync(m => m.WorkspaceId == workspaceId && m.DeletedAt == null);

        var projectIds = await _context.Projects
            .Where(p => p.WorkspaceId == workspaceId && p.DeletedAt == null)
            .Select(p => p.Id)
            .ToListAsync();

        var projectCount = projectIds.Count;

        var boardIds = await _context.Boards
            .Where(b => projectIds.Contains(b.ProjectId) && b.DeletedAt == null)
            .Select(b => b.Id)
            .ToListAsync();

        var columnIds = await _context.BoardColumns
            .Where(c => boardIds.Contains(c.BoardId) && c.DeletedAt == null)
            .Select(c => c.Id)
            .ToListAsync();

        var tasks = await _context.Tasks
            .Where(t => t.BoardColumnId != null && columnIds.Contains(t.BoardColumnId.Value) && t.DeletedAt == null)
            .Select(t => new { t.CompletedAt })
            .ToListAsync();

        var activeSprintCount = await _context.Sprints
            .CountAsync(s => projectIds.Contains(s.ProjectId) && s.Status == "active" && s.DeletedAt == null);

        var unreadNotifications = await _context.Notifications
            .CountAsync(n => n.UserId == userId && n.WorkspaceId == workspaceId && !n.IsRead && n.DeletedAt == null);

        return new WorkspaceStatisticsResponse
        {
            MemberCount = memberCount,
            ProjectCount = projectCount,
            TaskCount = tasks.Count,
            OpenTaskCount = tasks.Count(t => t.CompletedAt == null),
            CompletedTaskCount = tasks.Count(t => t.CompletedAt != null),
            ActiveSprintCount = activeSprintCount,
            UnreadNotificationCount = unreadNotifications
        };
    }

    public async System.Threading.Tasks.Task<ProjectStatisticsResponse> GetProjectStatisticsAsync(Guid projectId)
    {
        var boardIds = await _context.Boards
            .Where(b => b.ProjectId == projectId && b.DeletedAt == null)
            .Select(b => b.Id)
            .ToListAsync();

        var columnIds = await _context.BoardColumns
            .Where(c => boardIds.Contains(c.BoardId) && c.DeletedAt == null)
            .Select(c => c.Id)
            .ToListAsync();

        var tasks = await _context.Tasks
            .Where(t => t.BoardColumnId != null && columnIds.Contains(t.BoardColumnId.Value) && t.DeletedAt == null)
            .Select(t => new { t.CompletedAt, t.Priority })
            .ToListAsync();

        var memberCount = await _context.ProjectMembers
            .CountAsync(m => m.ProjectId == projectId && m.DeletedAt == null);

        var sprints = await _context.Sprints
            .Where(s => s.ProjectId == projectId && s.DeletedAt == null)
            .Select(s => s.Status)
            .ToListAsync();

        return new ProjectStatisticsResponse
        {
            BoardCount = boardIds.Count,
            MemberCount = memberCount,
            TaskCount = tasks.Count,
            OpenTaskCount = tasks.Count(t => t.CompletedAt == null),
            CompletedTaskCount = tasks.Count(t => t.CompletedAt != null),
            SprintCount = sprints.Count,
            ActiveSprintCount = sprints.Count(s => s == "active"),
            TasksByPriority = tasks
                .GroupBy(t => t.Priority)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async System.Threading.Tasks.Task<TaskStatisticsResponse> GetTaskStatisticsAsync(Guid projectId)
    {
        var columnIds = await GetProjectColumnIdsAsync(projectId);
        var today = DateOnly.FromDateTime(DateTime.UtcNow);

        var tasks = await _context.Tasks
            .Include(t => t.BoardColumn)
            .Where(t => t.BoardColumnId != null && columnIds.Contains(t.BoardColumnId.Value) && t.DeletedAt == null)
            .Select(t => new
            {
                t.Id,
                t.CompletedAt,
                t.DueDate,
                t.Priority,
                t.Type,
                StatusName = t.BoardColumn != null ? t.BoardColumn.Name : "Unassigned",
                HasAssignee = t.Assignees.Any(a => a.DeletedAt == null)
            })
            .ToListAsync();

        return new TaskStatisticsResponse
        {
            TotalTasks = tasks.Count,
            OpenTasks = tasks.Count(t => t.CompletedAt == null),
            CompletedTasks = tasks.Count(t => t.CompletedAt != null),
            OverdueTasks = tasks.Count(t =>
                t.CompletedAt == null && t.DueDate.HasValue && t.DueDate.Value < today),
            UnassignedTasks = tasks.Count(t => !t.HasAssignee),
            ByPriority = tasks.GroupBy(t => t.Priority).ToDictionary(g => g.Key, g => g.Count()),
            ByType = tasks.GroupBy(t => t.Type).ToDictionary(g => g.Key, g => g.Count()),
            ByStatus = tasks.GroupBy(t => t.StatusName).ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async System.Threading.Tasks.Task<SprintStatisticsResponse> GetSprintStatisticsAsync(Guid projectId)
    {
        var sprints = await _context.Sprints
            .Where(s => s.ProjectId == projectId && s.DeletedAt == null)
            .Select(s => new { s.Id, s.Status })
            .ToListAsync();

        var sprintIds = sprints.Select(s => s.Id).ToList();
        var tasksInSprints = sprintIds.Count == 0
            ? 0
            : await _context.Tasks.CountAsync(t => t.SprintId != null && sprintIds.Contains(t.SprintId.Value) && t.DeletedAt == null);

        var byStatus = sprints
            .GroupBy(s => s.Status)
            .ToDictionary(g => g.Key, g => g.Count());

        return new SprintStatisticsResponse
        {
            TotalSprints = sprints.Count,
            PlanningSprints = sprints.Count(s => s.Status == "planning"),
            ActiveSprints = sprints.Count(s => s.Status == "active"),
            CompletedSprints = sprints.Count(s => s.Status == "completed"),
            CancelledSprints = sprints.Count(s => s.Status == "cancelled"),
            TotalTasksInSprints = tasksInSprints,
            ByStatus = byStatus
        };
    }

    public async System.Threading.Tasks.Task<ActivitySummaryResponse> GetActivitySummaryAsync(Guid workspaceId)
    {
        var activities = await _context.Activities
            .Where(a => a.WorkspaceId == workspaceId)
            .Select(a => new { a.Action, a.EntityType, a.CreatedAt })
            .ToListAsync();

        var now = DateTime.UtcNow;
        var last7Days = now.AddDays(-7);
        var last30Days = now.AddDays(-30);

        return new ActivitySummaryResponse
        {
            TotalActivities = activities.Count,
            Last7DaysCount = activities.Count(a => a.CreatedAt >= last7Days),
            Last30DaysCount = activities.Count(a => a.CreatedAt >= last30Days),
            ByAction = activities.GroupBy(a => a.Action).ToDictionary(g => g.Key, g => g.Count()),
            ByEntityType = activities
                .Where(a => !string.IsNullOrEmpty(a.EntityType))
                .GroupBy(a => a.EntityType!)
                .ToDictionary(g => g.Key, g => g.Count())
        };
    }

    public async System.Threading.Tasks.Task<TimeTrackingStatisticsResponse> GetProjectTimeStatisticsAsync(Guid projectId)
    {
        var columnIds = await GetProjectColumnIdsAsync(projectId);

        var taskIds = await _context.Tasks
            .Where(t => t.BoardColumnId != null && columnIds.Contains(t.BoardColumnId.Value) && t.DeletedAt == null)
            .Select(t => t.Id)
            .ToListAsync();

        var timeLogs = await _context.TimeLogs
            .Include(tl => tl.User)
            .Include(tl => tl.Task)
            .Where(tl => taskIds.Contains(tl.TaskId) && tl.DeletedAt == null)
            .ToListAsync();

        var totalMinutes = timeLogs.Sum(tl => tl.DurationMinutes);

        var byUser = timeLogs
            .GroupBy(tl => tl.User != null ? (tl.User.DisplayName ?? tl.User.Email) : "Unknown")
            .ToDictionary(g => g.Key, g => g.Sum(tl => tl.DurationMinutes));

        var byTask = timeLogs
            .GroupBy(tl => tl.Task != null ? tl.Task.Title : "Unknown Task")
            .ToDictionary(g => g.Key, g => g.Sum(tl => tl.DurationMinutes));

        return new TimeTrackingStatisticsResponse
        {
            TotalDurationMinutes = totalMinutes,
            TotalEntries = timeLogs.Count,
            ByUser = byUser,
            ByTask = byTask
        };
    }

    public async System.Threading.Tasks.Task<ProjectReportResponse> GetProjectReportAsync(Guid projectId)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(p => p.Id == projectId && p.DeletedAt == null);

        var taskStats = await GetTaskStatisticsAsync(projectId);
        var sprintStats = await GetSprintStatisticsAsync(projectId);
        var timeStats = await GetProjectTimeStatisticsAsync(projectId);

        ActivitySummaryResponse activityStats = new();
        if (project != null)
        {
            activityStats = await GetActivitySummaryAsync(project.WorkspaceId);
        }

        return new ProjectReportResponse
        {
            ProjectId = projectId,
            ProjectName = project?.Name ?? "Project Report",
            TaskStats = taskStats,
            SprintStats = sprintStats,
            TimeStats = timeStats,
            ActivityStats = activityStats
        };
    }

    private async System.Threading.Tasks.Task<List<Guid>> GetProjectColumnIdsAsync(Guid projectId)
    {
        var boardIds = await _context.Boards
            .Where(b => b.ProjectId == projectId && b.DeletedAt == null)
            .Select(b => b.Id)
            .ToListAsync();

        return await _context.BoardColumns
            .Where(c => boardIds.Contains(c.BoardId) && c.DeletedAt == null)
            .Select(c => c.Id)
            .ToListAsync();
    }
}
