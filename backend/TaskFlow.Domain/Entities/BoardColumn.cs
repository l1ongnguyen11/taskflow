namespace TaskFlow.Domain.Entities;

public class BoardColumn : BaseEntity
{
    public Guid BoardId { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Position { get; set; }

    public string? Color { get; set; }

    public int? WipLimit { get; set; }

    public bool IsDoneColumn { get; set; }

    // Navigation Properties
    public Board Board { get; set; } = null!;

    public ICollection<Task> Tasks { get; set; } = new List<Task>();
}
