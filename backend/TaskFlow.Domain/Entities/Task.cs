namespace TaskFlow.Domain.Entities;

public class Task : BaseEntity
{
    public Guid? BoardColumnId { get; set; }

    public Guid? ParentTaskId { get; set; }

    public Guid? SprintId { get; set; }

    public Guid? ReporterId { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Description { get; set; }

    public string Type { get; set; } = string.Empty;

    public string Priority { get; set; } = string.Empty;

    public int TaskNumber { get; set; }

    public int Position { get; set; }

    public short? StoryPoints { get; set; }

    public DateOnly? StartDate { get; set; }

    public DateOnly? DueDate { get; set; }

    public DateTime? CompletedAt { get; set; }

    // Navigation Properties
    public BoardColumn? BoardColumn { get; set; }

    public Task? ParentTask { get; set; }

    public Sprint? Sprint { get; set; }

    public User? Reporter { get; set; }

    public ICollection<Task> SubTasks { get; set; } = new List<Task>();

    public ICollection<TaskAssignee> Assignees { get; set; } = new List<TaskAssignee>();

    public ICollection<TaskWatcher> Watchers { get; set; } = new List<TaskWatcher>();

    public ICollection<TaskDependency> Dependencies { get; set; } = new List<TaskDependency>();

    public ICollection<TaskDependency> Dependents { get; set; } = new List<TaskDependency>();

    public ICollection<TaskLabel> TaskLabels { get; set; } = new List<TaskLabel>();

    public ICollection<Checklist> Checklists { get; set; } = new List<Checklist>();

    public ICollection<Comment> Comments { get; set; } = new List<Comment>();

    public ICollection<TaskAttachment> Attachments { get; set; } = new List<TaskAttachment>();

    public ICollection<TimeLog> TimeLogs { get; set; } = new List<TimeLog>();
}
