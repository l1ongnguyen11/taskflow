namespace TaskFlow.Domain.Entities;

public class Checklist : BaseEntity
{
    public Guid TaskId { get; set; }

    public string Title { get; set; } = string.Empty;

    public int Position { get; set; }

    // Navigation Properties
    public Task Task { get; set; } = null!;

    public ICollection<ChecklistItem> Items { get; set; } = new List<ChecklistItem>();
}
