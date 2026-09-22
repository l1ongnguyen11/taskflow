using System.Collections.Generic;

namespace TaskFlow.Application.DTOs.Dashboard;

public class ProjectStatisticsResponse
{
    public int BoardCount { get; set; }
    public int MemberCount { get; set; }
    public int TaskCount { get; set; }
    public int OpenTaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
    public int SprintCount { get; set; }
    public int ActiveSprintCount { get; set; }
    public Dictionary<string, int> TasksByPriority { get; set; } = new();
}
