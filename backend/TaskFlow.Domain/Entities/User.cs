namespace TaskFlow.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;

    public string PasswordHash { get; set; } = string.Empty;

    public string DisplayName { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public bool IsActive { get; set; }

    // Navigation Properties
    public ICollection<WorkspaceMember> WorkspaceMemberships { get; set; } = new List<WorkspaceMember>();

    public ICollection<ProjectMember> ProjectMemberships { get; set; } = new List<ProjectMember>();

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();

    public ICollection<TaskAssignee> TaskAssignees { get; set; } = new List<TaskAssignee>();

    public ICollection<TaskWatcher> TaskWatchers { get; set; } = new List<TaskWatcher>();

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public ICollection<TimeLog> TimeLogs { get; set; } = new List<TimeLog>();

    public ICollection<Activity> Activities { get; set; } = new List<Activity>();
}