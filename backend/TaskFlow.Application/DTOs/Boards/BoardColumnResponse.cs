using System;

namespace TaskFlow.Application.DTOs.Boards;

public class BoardColumnResponse
{
    public Guid Id { get; set; }
    public Guid BoardId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Position { get; set; }
    public string? Color { get; set; }
    public int? WipLimit { get; set; }
    public bool IsDoneColumn { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
