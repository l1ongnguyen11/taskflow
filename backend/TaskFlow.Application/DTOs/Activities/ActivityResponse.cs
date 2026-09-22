using System;

namespace TaskFlow.Application.DTOs.Activities;

public class ActivityResponse
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid? ActorId { get; set; }
    public string? ActorDisplayName { get; set; }
    public string? ActorAvatarUrl { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public Guid EntityId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public DateTime CreatedAt { get; set; }
}
