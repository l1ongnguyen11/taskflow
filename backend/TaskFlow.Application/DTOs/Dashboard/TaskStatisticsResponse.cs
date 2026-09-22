using System.Collections.Generic;

namespace TaskFlow.Application.DTOs.Dashboard;

public class TaskStatisticsResponse
{
    public int TotalTasks { get; set; }
    public int OpenTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int UnassignedTasks { get; set; }
    public Dictionary<string, int> ByPriority { get; set; } = new();
    public Dictionary<string, int> ByType { get; set; } = new();
    public Dictionary<string, int> ByStatus { get; set; } = new();
}
