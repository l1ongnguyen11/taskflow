namespace TaskFlow.Domain.Entities;

public class Board : BaseEntity
{
    public Guid ProjectId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    // Navigation Properties
    public Project Project { get; set; } = null!;

    public ICollection<BoardColumn> Columns { get; set; } = new List<BoardColumn>();
}
