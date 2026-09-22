using System;
using System.Collections.Generic;

namespace TaskFlow.Application.DTOs.Tasks;

public class TaskResponse
{
    public Guid Id { get; set; }
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
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<TaskUserDto> Assignees { get; set; } = new();
    public List<TaskUserDto> Watchers { get; set; } = new();
    public List<TaskDependencyDto> Dependencies { get; set; } = new();
    public List<TaskLabelDto> Labels { get; set; } = new();
}

public class TaskUserDto
{
    public Guid Id { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? AvatarUrl { get; set; }
}

public class TaskDependencyDto
{
    public Guid DependsOnId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int TaskNumber { get; set; }
    public string Type { get; set; } = string.Empty;
}

public class TaskLabelDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = string.Empty;
}
