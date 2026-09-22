namespace TaskFlow.Domain.Entities;

public class TaskDependency : BaseEntity
{
    public Guid TaskId { get; set; }

    public Guid DependsOnId { get; set; }

    public string Type { get; set; } = string.Empty;

    // Navigation Properties
    public Task Task { get; set; } = null!;

    public Task DependsOn { get; set; } = null!;
}
