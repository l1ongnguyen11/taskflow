using System.Collections.Generic;

namespace TaskFlow.Application.DTOs.Dashboard;

public class ActivitySummaryResponse
{
    public int TotalActivities { get; set; }
    public int Last7DaysCount { get; set; }
    public int Last30DaysCount { get; set; }
    public Dictionary<string, int> ByAction { get; set; } = new();
    public Dictionary<string, int> ByEntityType { get; set; } = new();
}
