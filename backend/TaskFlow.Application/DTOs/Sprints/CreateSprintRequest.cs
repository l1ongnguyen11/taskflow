using System;

namespace TaskFlow.Application.DTOs.Sprints;

public class CreateSprintRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Goal { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
}
