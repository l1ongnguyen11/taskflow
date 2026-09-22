using System;

namespace TaskFlow.Application.DTOs.Files;

public class TaskAttachmentResponse
{
    public Guid Id { get; set; }
    public Guid TaskId { get; set; }
    public Guid FileId { get; set; }
    public FileResponse File { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
