using System;

namespace TaskFlow.Application.DTOs.Checklists;

public class CreateChecklistItemRequest
{
    public string Content { get; set; } = string.Empty;
    public Guid? AssigneeId { get; set; }
}
