using System;
using System.Collections.Generic;
using TaskFlow.Application.Common;

namespace TaskFlow.Application.DTOs.TimeLogs;

public class TimeReportUserSummary
{
    public Guid? UserId { get; set; }
    public string? UserDisplayName { get; set; }
    public int TotalDurationMinutes { get; set; }
    public int EntryCount { get; set; }
}

public class TimeReportResponse
{
    public Guid TaskId { get; set; }
    public DateTime? From { get; set; }
    public DateTime? To { get; set; }
    public int TotalDurationMinutes { get; set; }
    public int TotalEntries { get; set; }
    public List<TimeReportUserSummary> ByUser { get; set; } = new();
    public List<TimeLogResponse> Entries { get; set; } = new();
    public PaginationMetadata Pagination { get; set; } = new();
}
