using System;

namespace TaskFlow.Application.DTOs.Dashboard;

public class ProjectReportResponse
{
    public Guid ProjectId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public TaskStatisticsResponse TaskStats { get; set; } = new();
    public SprintStatisticsResponse SprintStats { get; set; } = new();
    public TimeTrackingStatisticsResponse TimeStats { get; set; } = new();
    public ActivitySummaryResponse ActivityStats { get; set; } = new();
}
