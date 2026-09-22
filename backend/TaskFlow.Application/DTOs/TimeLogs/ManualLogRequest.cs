using System;

namespace TaskFlow.Application.DTOs.TimeLogs;

public class ManualLogRequest
{
    public string? Description { get; set; }
    public DateTime StartedAt { get; set; }
    public int DurationMinutes { get; set; }
}
