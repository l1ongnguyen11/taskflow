namespace TaskFlow.Domain.Entities;

public class TaskAttachment : BaseEntity
{
    public Guid TaskId { get; set; }

    public Guid FileId { get; set; }

    // Navigation Properties
    public Task Task { get; set; } = null!;

    public File File { get; set; } = null!;
}
