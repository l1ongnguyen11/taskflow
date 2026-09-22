namespace TaskFlow.Domain.Entities;

public class Label : BaseEntity
{
    public Guid WorkspaceId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Color { get; set; } = string.Empty;

    // Navigation Properties
    public Workspace Workspace { get; set; } = null!;

    public ICollection<TaskLabel> TaskLabels { get; set; } = new List<TaskLabel>();
}
