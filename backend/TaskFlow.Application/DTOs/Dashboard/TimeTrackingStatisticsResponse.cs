using System.Collections.Generic;

namespace TaskFlow.Application.DTOs.Dashboard;

public class TimeTrackingStatisticsResponse
{
    public int TotalDurationMinutes { get; set; }
    public int TotalEntries { get; set; }
    public Dictionary<string, int> ByUser { get; set; } = new();
    public Dictionary<string, int> ByTask { get; set; } = new();
}
