namespace TaskFlow.Domain.Entities;

public class Project : BaseEntity
{
    public Guid WorkspaceId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Key { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid? LeadId { get; set; }

    public bool IsArchived { get; set; }

    // Navigation Properties
    public Workspace Workspace { get; set; } = null!;

    public User? Lead { get; set; }

    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();

    public ICollection<Board> Boards { get; set; } = new List<Board>();

    public ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();
}
