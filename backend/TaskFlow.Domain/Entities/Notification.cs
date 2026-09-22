namespace TaskFlow.Domain.Entities;

public class Notification : BaseEntity
{
    public Guid UserId { get; set; }

    public Guid? WorkspaceId { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Body { get; set; }

    public string? EntityType { get; set; }

    public Guid? EntityId { get; set; }

    public bool IsRead { get; set; }

    // Navigation Properties
    public User User { get; set; } = null!;

    public Workspace? Workspace { get; set; }
}
