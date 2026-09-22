namespace TaskFlow.Domain.Entities;

public class WorkspaceMember : BaseEntity
{
    public Guid WorkspaceId { get; set; }

    public Guid UserId { get; set; }

    public DateTime JoinedAt { get; set; }

    // Navigation Properties
    public Workspace Workspace { get; set; } = null!;

    public User User { get; set; } = null!;
}
