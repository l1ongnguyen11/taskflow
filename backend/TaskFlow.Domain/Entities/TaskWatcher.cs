namespace TaskFlow.Domain.Entities;

public class TaskWatcher : BaseEntity
{
    public Guid TaskId { get; set; }

    public Guid UserId { get; set; }

    // Navigation Properties
    public Task Task { get; set; } = null!;

    public User User { get; set; } = null!;
}
