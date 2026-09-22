using System;

namespace TaskFlow.Application.DTOs.TimeLogs;

public class TimeLogResponse
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid? UserId { get; set; }
    public string? UserDisplayName { get; set; }
    public string? Description { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public int DurationMinutes { get; set; }
    public bool IsRunning { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
