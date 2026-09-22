namespace TaskFlow.Domain.Entities;

public class Comment : BaseEntity
{
    public Guid TaskId { get; set; }

    public Guid? UserId { get; set; }

    public Guid? ParentCommentId { get; set; }

    public string Body { get; set; } = string.Empty;

    // Navigation Properties
    public Task Task { get; set; } = null!;

    public User? User { get; set; }

    public Comment? ParentComment { get; set; }

    public ICollection<Comment> Replies { get; set; } = new List<Comment>();
}
