using System;
using System.Collections.Generic;
using System.IO;
using TaskFlow.Application.Common;
using TaskFlow.Application.DTOs.Files;

namespace TaskFlow.Application.Interfaces.Services;

public interface IFileService
{
    System.Threading.Tasks.Task<ApiResponse<FileResponse>> UploadAsync(Guid userId, Stream stream, string fileName, string contentType, long sizeBytes);
    System.Threading.Tasks.Task<(ApiResponse<FileResponse>? Error, Stream? Stream, string? ContentType, string? FileName)> DownloadAsync(Guid userId, Guid fileId);
    System.Threading.Tasks.Task<ApiResponse<object>> DeleteAsync(Guid userId, Guid fileId);
    System.Threading.Tasks.Task<ApiResponse<TaskAttachmentResponse>> AttachToTaskAsync(Guid userId, Guid taskId, AttachFileToTaskRequest request);
    System.Threading.Tasks.Task<ApiResponse<List<TaskAttachmentResponse>>> GetTaskAttachmentsAsync(Guid userId, Guid taskId);
    System.Threading.Tasks.Task<ApiResponse<object>> DetachFromTaskAsync(Guid userId, Guid taskId, Guid attachmentId);
}
