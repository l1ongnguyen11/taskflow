namespace TaskFlow.Domain.Entities;

/// <summary>
/// Polymorphic audit log for all entity changes across the workspace.
/// Immutable — no UpdatedAt or DeletedAt in the schema.
/// </summary>
public class Activity : BaseEntity
{
    public Guid WorkspaceId { get; set; }

    public Guid? ActorId { get; set; }

    public string EntityType { get; set; } = string.Empty;

    public Guid EntityId { get; set; }

    public string Action { get; set; } = string.Empty;

    public string? OldValue { get; set; }

    public string? NewValue { get; set; }

    // Navigation Properties
    public Workspace Workspace { get; set; } = null!;

    public User? Actor { get; set; }
}
