using System.Collections.Generic;

namespace TaskFlow.Application.DTOs.Dashboard;

public class SprintStatisticsResponse
{
    public int TotalSprints { get; set; }
    public int PlanningSprints { get; set; }
    public int ActiveSprints { get; set; }
    public int CompletedSprints { get; set; }
    public int CancelledSprints { get; set; }
    public int TotalTasksInSprints { get; set; }
    public Dictionary<string, int> ByStatus { get; set; } = new();
}
