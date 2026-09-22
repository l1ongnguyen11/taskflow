using System;
using System.Collections.Generic;

namespace TaskFlow.Application.DTOs.Boards;

public class BoardResponse
{
    public Guid Id { get; set; }
    public Guid ProjectId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<BoardColumnResponse> Columns { get; set; } = new();
}
