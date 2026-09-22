namespace TaskFlow.Domain.Entities;

public class ChecklistItem : BaseEntity
{
    public Guid ChecklistId { get; set; }

    public string Content { get; set; } = string.Empty;

    public bool IsCompleted { get; set; }

    public int Position { get; set; }

    public Guid? AssigneeId { get; set; }

    // Navigation Properties
    public Checklist Checklist { get; set; } = null!;

    public User? Assignee { get; set; }
}
