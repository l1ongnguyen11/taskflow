using System;

namespace TaskFlow.Application.DTOs.Tasks;

public class MoveTaskRequest
{
    public Guid TargetColumnId { get; set; }
    public int Position { get; set; }
}
