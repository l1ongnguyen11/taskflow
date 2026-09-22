using System;

namespace TaskFlow.Application.DTOs.Sprints;

public class UpdateSprintRequest
{
    public string? Name { get; set; }
    public string? Goal { get; set; }
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? Status { get; set; }
}
