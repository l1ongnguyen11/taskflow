using System.Collections.Generic;

namespace TaskFlow.Application.DTOs.Dashboard;

public class WorkspaceStatisticsResponse
{
    public int MemberCount { get; set; }
    public int ProjectCount { get; set; }
    public int TaskCount { get; set; }
    public int ActiveSprintCount { get; set; }
    public int UnreadNotificationCount { get; set; }
    public int OpenTaskCount { get; set; }
    public int CompletedTaskCount { get; set; }
}
