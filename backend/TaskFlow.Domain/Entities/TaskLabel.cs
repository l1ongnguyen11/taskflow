namespace TaskFlow.Domain.Entities;

public class TaskLabel : BaseEntity
{
    public Guid TaskId { get; set; }

    public Guid LabelId { get; set; }

    // Navigation Properties
    public Task Task { get; set; } = null!;

    public Label Label { get; set; } = null!;
}
