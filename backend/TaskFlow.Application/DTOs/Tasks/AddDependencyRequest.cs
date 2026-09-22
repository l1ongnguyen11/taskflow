using System;

namespace TaskFlow.Application.DTOs.Tasks;

public class AddDependencyRequest
{
    public Guid DependsOnId { get; set; }
    public string Type { get; set; } = "finish_to_start";
}
