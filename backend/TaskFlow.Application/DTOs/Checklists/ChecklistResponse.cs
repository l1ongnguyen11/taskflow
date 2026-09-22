using System;
using System.Collections.Generic;

namespace TaskFlow.Application.DTOs.Checklists;

public class ChecklistResponse
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int Position { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<ChecklistItemResponse> Items { get; set; } = new();
}

public class ChecklistItemResponse
{
    public Guid Id { get; set; }
    public Guid ChecklistId { get; set; }
    public string Content { get; set; } = string.Empty;
    public bool IsCompleted { get; set; }
    public int Position { get; set; }
    public Guid? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
