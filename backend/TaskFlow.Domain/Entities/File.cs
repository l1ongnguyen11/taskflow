namespace TaskFlow.Domain.Entities;

public class File : BaseEntity
{
    public string OriginalName { get; set; } = string.Empty;

    public string StoredPath { get; set; } = string.Empty;

    public string MimeType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }

    public Guid? UploadedBy { get; set; }

    // Navigation Properties
    public User? Uploader { get; set; }

    public ICollection<TaskAttachment> TaskAttachments { get; set; } = new List<TaskAttachment>();
}
