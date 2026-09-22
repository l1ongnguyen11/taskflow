namespace TaskFlow.Domain.Entities;

public class TimeLog : BaseEntity
{
    public Guid TaskId { get; set; }

    public Guid? UserId { get; set; }

    public string? Description { get; set; }

    public DateTime StartedAt { get; set; }

    public DateTime? EndedAt { get; set; }

    public int DurationMinutes { get; set; }

    // Navigation Properties
    public Task Task { get; set; } = null!;

    public User? User { get; set; }
}
