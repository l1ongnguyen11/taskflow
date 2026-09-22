namespace TaskFlow.Domain.Entities;

public class Invitation : BaseEntity
{
    public Guid WorkspaceId { get; set; }

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = "member";

    public Guid? InvitedBy { get; set; }

    public string Token { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;

    public DateTime ExpiresAt { get; set; }

    // Navigation Properties
    public Workspace Workspace { get; set; } = null!;

    public User? Inviter { get; set; }
}
