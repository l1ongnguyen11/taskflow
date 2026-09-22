using System;

namespace TaskFlow.Application.DTOs.Files;

public class FileResponse
{
    public Guid Id { get; set; }
    public string OriginalName { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public long SizeBytes { get; set; }
    public Guid? UploadedBy { get; set; }
    public string? DownloadUrl { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}
