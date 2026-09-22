namespace TaskFlow.Domain.Entities;

public class Workspace : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public string Slug { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string? LogoUrl { get; set; }

    public Guid? CreatedBy { get; set; }

    // Navigation Properties
    public User? Creator { get; set; }

    public ICollection<WorkspaceMember> Members { get; set; } = new List<WorkspaceMember>();

    public ICollection<Invitation> Invitations { get; set; } = new List<Invitation>();

    public ICollection<Project> Projects { get; set; } = new List<Project>();

    public ICollection<Label> Labels { get; set; } = new List<Label>();

    public ICollection<Activity> Activities { get; set; } = new List<Activity>();

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
}
