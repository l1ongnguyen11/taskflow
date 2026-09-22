using System;

namespace TaskFlow.Application.DTOs.Checklists;

public class UpdateChecklistItemRequest
{
    public string Content { get; set; } = string.Empty;
    public int Position { get; set; }
    public Guid? AssigneeId { get; set; }
}
