namespace TaskFlow.Domain.Entities;

public class Sprint : BaseEntity
{
    public Guid ProjectId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Goal { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? EndDate { get; set; }

    public string Status { get; set; } = string.Empty;

    // Navigation Properties
    public Project Project { get; set; } = null!;

    public ICollection<Task> Tasks { get; set; } = new List<Task>();
}
