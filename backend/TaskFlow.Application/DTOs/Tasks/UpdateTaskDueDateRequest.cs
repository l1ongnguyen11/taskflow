using System;

namespace TaskFlow.Application.DTOs.Tasks;

public class UpdateTaskDueDateRequest
{
    public DateOnly? DueDate { get; set; }
}
