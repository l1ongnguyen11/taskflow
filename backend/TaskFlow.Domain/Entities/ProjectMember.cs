namespace TaskFlow.Domain.Entities;

public class ProjectMember : BaseEntity
{
    public Guid ProjectId { get; set; }

    public Guid UserId { get; set; }

    public string Role { get; set; } = "member";

    // Navigation Properties
    public Project Project { get; set; } = null!;

    public User User { get; set; } = null!;
}
